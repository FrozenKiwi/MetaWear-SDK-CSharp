using MbientLab.MetaWear.Impl.Platform;
using System;
using System.Threading.Tasks;
using Android.Bluetooth;
using System.Globalization;
using Android.Content;
using MbientLab.MetaWear.Impl;
using Java.Lang;
using System.Threading;
using Java.Util;
using System.Linq;

namespace MetaWear.Android
{
    public partial class BluetoothLeGatt : IBluetoothLeGatt
    {
        private BluetoothDevice btDevice;
        private readonly Context context;
        private BluetoothGatt androidBtGatt;

        private readonly TimedTask<bool> connectTask = new TimedTask<bool>();
        private TaskCompletionSource<bool> disconnectTaskSrc = null;

        private Tuple<Guid, Guid> notifyChar = null;
        private Action<byte[]> notificationListener;

        // Current task being executed.
        internal volatile int nGattOps = 0;
        public TimedTask<byte[]> gattOpTask = new TimedTask<byte[]>();
        static readonly BluetoothLeGattQueue queue = new BluetoothLeGattQueue();


        public ulong BluetoothAddress => UInt32.Parse(btDevice.Address, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

        public Action OnDisconnect { get; set; }

        private readonly BluetoothLeGattCallback callback;

        public BluetoothLeGatt(BluetoothDevice device, Context context)
        {
            btDevice = device;
            this.context = context;
            callback = new BluetoothLeGattCallback(this);

            //IntentFilter filter = new IntentFilter();
            //filter.AddAction(BluetoothDevice.ActionAclConnected);
            //filter.AddAction(BluetoothDevice.ActionAclDisconnected);
            //RegisterReceiver(this, filter);

            //device. += (sender, args) =>
            //{
            //    switch (sender.ConnectionStatus)
            //    {
            //        case BluetoothConnectionStatus.Disconnected:
            //            ResetCharacteristics();
            //            OnDisconnect();
            //            break;
            //        case BluetoothConnectionStatus.Connected:
            //            break;
            //    }
            //};
        }

        public async Task DisconnectAsync()
        {
            while (nGattOps > 0)
                await Task.Delay(1000);

            if (androidBtGatt != null)
            {
                androidBtGatt.Disconnect();
            }
        }

        public Task DiscoverServicesAsync()
        {
            return (androidBtGatt == null) ?
                Task.Run(() =>
                {
                    androidBtGatt = btDevice.ConnectGatt(context, false, callback);
                    androidBtGatt.DiscoverServices();
                }) :
                Task.CompletedTask;
        }

        public Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler)
        {
            return EditNotifications(gattChar, handler);
        }

        private Java.Util.UUID ToUUID(Guid guid) => Java.Util.UUID.FromString(guid.ToString());

        BluetoothGattCharacteristic GetCharacteristic(Tuple<Guid, Guid> gattChar)
        {
            if (androidBtGatt == null)
            {
                throw new IllegalStateException("Not connected to the BTLE gatt server");
            }

            var sid = ToUUID(gattChar.Item1);
            var cid = ToUUID(gattChar.Item2);
            BluetoothGattService service = androidBtGatt.GetService(sid);

            if (service == null)
            {
                throw new IllegalStateException("Service \'" + sid.ToString() + "\' does not exist");
            }

            BluetoothGattCharacteristic androidGattChar = service.GetCharacteristic(cid);
            if (androidGattChar == null)
            {
                throw new IllegalStateException("Characteristic \'" + cid.ToString() + "\' does not exist");
            }
            return androidGattChar;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceGuid"></param>
        /// <returns></returns>
        public Task<bool> ServiceExistsAsync(Guid serviceGuid)
        {
            var uuid = ToUUID(serviceGuid);
            var r = androidBtGatt != null && androidBtGatt.GetService(uuid) != null;
            return Task.FromResult(r);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gattChar"></param>
        /// <returns></returns>
        public Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar)
        {
            var androidGattChar = GetCharacteristic(gattChar);

            return queue.AddGattOperation(
                this, 
                "onCharacteristicRead not called within %dms", 
                ()=> Task.FromResult(androidBtGatt.ReadCharacteristic(androidGattChar)));

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gattChar"></param>
        /// <param name="writeType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value)
        {
            var androidGattChar = GetCharacteristic(gattChar);

            return queue.AddGattOperation(this, "onCharacteristicWrite not called within %dms", () => {
                androidGattChar.WriteType = writeType == GattCharWriteType.WRITE_WITHOUT_RESPONSE ?
                        GattWriteType.NoResponse :
                        GattWriteType.Default;

                androidGattChar.SetValue(value);

                androidBtGatt.WriteCharacteristic(androidGattChar);
                return Task.CompletedTask;
            });
        }

        ////

        void Disconnected(GattStatus status)
        {
            CloseGatt();

            if (!connectTask.Completed && status != 0)
            {
                connectTask.SetError(new IllegalStateException(string.Format("Non-zero onConnectionStateChange status (%s)", status)));
            }
            else
            {
                if (disconnectTaskSrc == null || disconnectTaskSrc.Task.IsCompleted)
                {
                    // TODO: Handle this?
                    //dcHandler.onUnexpectedDisconnect(status);
                }
                else
                {
                    OnDisconnect(); // dcHandler.onDisconnect();
                    disconnectTaskSrc.SetResult(true);
                }
            }
        }

        internal void GattTaskCompleted()
        {
            int count = Interlocked.Decrement(ref nGattOps);
        }

        bool Refresh()
        {
            try
            {
                androidBtGatt.Class.GetMethod("refresh").Invoke(androidBtGatt);
                return true;
            }
            catch (Java.Lang.Exception e)
            {
                //Log.w(LOG_TAG, "Error refreshing gatt cache", e);
                return false;
            }
        }

        internal void CloseGatt()
        {
            if (androidBtGatt != null)
            {
                Refresh();
                androidBtGatt.Close();
                androidBtGatt = null;
            }
        }

        private static UUID CHARACTERISTIC_CONFIG = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        private Task<byte[]> EditNotifications(Tuple<Guid, Guid> gattChar, Action<byte[]> handler)
        {
            var androidGattChar = GetCharacteristic(gattChar);
            if (androidGattChar.Properties.HasFlag(GattProperty.Notify))
            {
                return queue.AddGattOperation(this, "onDescriptorWrite not called within %dms", () => {
                    androidBtGatt.SetCharacteristicNotification(androidGattChar, true);
                    BluetoothGattDescriptor descriptor = androidGattChar.GetDescriptor(CHARACTERISTIC_CONFIG);

                    var enableVal = handler == null ? BluetoothGattDescriptor.DisableNotificationValue : BluetoothGattDescriptor.EnableIndicationValue;
                    descriptor.SetValue(enableVal.ToArray());
                    androidBtGatt.WriteDescriptor(descriptor);

                    notificationListener = handler;
                    return Task.FromResult<byte[]>(null);
                });
            }
            throw new IllegalStateException("Characteristic does not have 'notify property' bit set");
        }
    }
}
