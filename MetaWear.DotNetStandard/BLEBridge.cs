using MbientLab.MetaWear.Platform;
using System;
using System.Threading.Tasks;
using Plugin.BluetoothLE;
using ReactiveUI;

using System.Reactive.Linq;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;

namespace MetaWear.NetStandard
{
    using CharIdent = Tuple<Guid, Guid>;

    public class BLEBridge : IBluetoothLeGatt
    {
        private IDevice device;
        private object connection;

        private Dictionary<CharIdent, IGattCharacteristic> characteristics;
        //private Dictionary<CharIdent, IGattCharacteristic> _characteristics;


        public BLEBridge(MWDevice mwdevice)
        {
            device = mwdevice.device;
            Plugin.BluetoothLE.Log.MinLogLevel = LogLevel.Debug;
            Plugin.BluetoothLE.Log.Out = (string s, string msg, Plugin.BluetoothLE.LogLevel level) =>
            {
                // TODO: Proper logging?
                var threadId = Thread.CurrentThread.ManagedThreadId;
                System.Diagnostics.Debug.WriteLine("{0} - {1}: {2}", threadId, level, msg);
            };
        }

        public Task DiscoverServicesAsync()
        {
            return device.WhenServiceDiscovered().FirstOrDefaultAsync().ToTask();
        }


        internal async Task connectToDevice()
        {
            device.WhenStatusChanged().Subscribe(status =>
            {
                switch (status)
                {
                    case ConnectionStatus.Disconnected:
                        _notification?.Dispose();
                        _notification = null;
                        if (dcTaskSource != null)
                        {
                            dcTaskSource.TrySetResult(true);
                            OnDisconnect?.Invoke();
                        }
                        else
                        {
                            OnDisconnect?.Invoke();
                        }
                        break;
                };
            });


            using (var cancelSrc = new CancellationTokenSource())
            {
                //using (this.Dialogs.Loading("Connecting", cancelSrc.Cancel, "Cancel"))
                {
                    await this.device.Connect();
                }
            }

            // Cache all characteristics?
            characteristics = new Dictionary<CharIdent, IGattCharacteristic>();
            //_characteristics = new Dictionary<CharIdent, IGattCharacteristic>();
            //device.WhenAnyCharacteristicDiscovered().Subscribe(ch =>
            //{
            //    CharIdent ident = new CharIdent(ch.Service.Uuid, ch.Uuid);
            //    _characteristics.Add(ident, ch);
            //});
        }

        public ulong BluetoothAddress => 0; // TODO

        public Action OnDisconnect { get; set; }

        private async Task<IGattCharacteristic> GetGattCharacteristic(Tuple<Guid, Guid> gattChar)
        {
            IGattService service = await device.GetKnownService(gattChar.Item1);
            IGattCharacteristic result = await service.GetKnownCharacteristics(gattChar.Item2);
            return result;
            //IGattCharacteristic result = null;
            //if (characteristics.TryGetValue(gattChar, out result))
            //    return result;


            //result = await device.GetKnownCharacteristics(gattChar.Item1, gattChar.Item2);
            //if (result.Service.Uuid != gattChar.Item1)
            //    throw new Exception("Bad Service Found");
            //characteristics.Add(gattChar, result);
            //return result;
        }

        IDisposable _notification;
        public async Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler)
        {
            var ch = await GetGattCharacteristic(gattChar);
            var res = await ch.EnableNotifications();
            if (res)
            {
                _notification = ch.WhenNotificationReceived().Subscribe(x => handler(x.Data));
            }
        }

        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar)
        {
            var ch = await GetGattCharacteristic(gattChar);
            if (ch != null)
            {
                var res = await ch.Read();
                return res.Data;
            }
            return new byte[0];
        }

        private TaskCompletionSource<bool> dcTaskSource;
        public Task<bool> RemoteDisconnectAsync()
        {
            // Copied from Win10 implementation
            dcTaskSource = new TaskCompletionSource<bool>();
            return dcTaskSource.Task;
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceGuid)
        {
            var service = await device.GetKnownService(serviceGuid).FirstOrDefaultAsync();
            return service != null;
        }

        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value)
        {
            var ch = await GetGattCharacteristic(gattChar);
            bool canWriteWithResp = ch.CanWriteWithResponse();
            bool canWriteSansResp = ch.CanWriteWithoutResponse();
            if (writeType == GattCharWriteType.WRITE_WITH_RESPONSE || !canWriteSansResp)
            //if (ch.CanWriteWithResponse())
            {
                var results = await ch.BlobWrite(value).SingleOrDefaultAsync();
                //if (resp.S)
                // Note - whaddayou do with a failed write?
            }
            else
                ch.WriteWithoutResponse(value);
        }

        public Task DisconnectAsync()
        {
            device?.CancelConnection();
            return Task.CompletedTask;
        }
    }
}
