using MbientLab.MetaWear.Platform;
using System;
using System.Threading.Tasks;
using Plugin.BluetoothLE;
using ReactiveUI;

using System.Reactive.Linq;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace MetaWear.NetStandard
{
    using CharIdent = Tuple<Guid, Guid>;

    public class BLEBridge : IBluetoothLeGatt
    {
        private IDevice device;
        private object connection;
        private ConcurrentDictionary<CharIdent, IGattCharacteristic> characteristics;


        public static Action<string /*category*/, string /*message*/, int /*severity*/> BleLogging;
        public static int LogLevel
        {
            get { return (int)Log.MinLogLevel; }
            set { Log.MinLogLevel = (Plugin.BluetoothLE.LogLevel)value; }
        }

#if _ANDROID
        public AndroidPerformActionsOnMainThread => CrossBleAdapter.AndroidPerformActionsOnMainThread;
#endif

        public BLEBridge(MWDevice mwdevice)
        {
            device = mwdevice.device;
            Log.Out = (string s, string msg, LogLevel level) =>
            {
                BleLogging?.Invoke(s, msg, (int)level);
            };
        }

        public async Task DiscoverServicesAsync()
        {
            await connectToDevice();
            // We cannot await the below function, as it does not complete
            //device.WhenAnyCharacteristicDiscovered().Subscribe((ch) =>
            //{
            //    characteristics.TryAdd(new CharIdent(ch.Service.Uuid, ch.Uuid), ch);
            //});
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
                    connection = await this.device.Connect();
                }
            }

            // Cache all characteristics?
            characteristics = new ConcurrentDictionary<CharIdent, IGattCharacteristic>();
        }

        public ulong BluetoothAddress => 0; // TODO

        public Action OnDisconnect { get; set; }

        private async Task<IGattCharacteristic> GetGattCharacteristic(Tuple<Guid, Guid> gattChar)
        {
            IGattCharacteristic result = null;
            // NOTE - caching the characteristics breaks notifications (streaming doesn't work).
            if (!characteristics.TryGetValue(gattChar, out result))
            {
                IGattService service = await device.GetKnownService(gattChar.Item1);
                result = await service.GetKnownCharacteristics(gattChar.Item2);
            //    characteristics.TryAdd(gattChar, result);
            }
            return result;
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
            {
                //Log.Debug("chwr", "RespWriting: " + ByteArrayToString(value));
                var results = await ch.Write(value);
                if (results.Data != value)
                {
                    //Log.Debug("chwr", "Writing Failed: " + ByteArrayToString(results.Data));
                }
            }
            else
            {
                //Log.Debug("chwr", "Writing: " + ByteArrayToString(value));
                ch.WriteWithoutResponse(value);
            }
        }

        public Task DisconnectAsync()
        {
            device?.CancelConnection();
            return Task.CompletedTask;
        }
    }
}
