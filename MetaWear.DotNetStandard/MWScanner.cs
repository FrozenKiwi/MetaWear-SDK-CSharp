using Plugin.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaWear.NetStandard
{
    public class MWScanner
    {
        private Dictionary<Guid, MWDevice> seenDevices = new Dictionary<Guid, MWDevice>();
        private IDisposable scan;

        public void ClearScanResults()
        {
            StopScanning();
            seenDevices.Clear();
        }

        public void AddDevice(MWDevice device) { seenDevices.Add(device.Uuid, device); }

        private bool matches(IDevice device)
        {
            // TODO: Verify this is actually a MetaWear device
            //var uuids = scanResult.AdvertisementData.ServiceUuids;
            //foreach (Guid uuid in uuids)
            //{
            //    var st = uuid.ToString();
            //}
            //var s = scanResult.AdvertisementData.ToString();
            //var serviceGuid = config.ServiceUuids.First();
            //var service = await scanResult.Device.GetKnownService(serviceGuid).FirstOrDefaultAsync();
            return (device.Name == "MetaWear");
        }

        public async Task<bool> StartScanning(Func<MWDevice, Task<bool>> whenFound)
        {
            StopScanning();
            var adapter = CrossBleAdapter.Current;

            if (adapter == null)
            {
                BLEBridge.BleLogging("No adaptor present", "perhaps it is not turned on?", 3);
                return false;
            }
            if (adapter.Status != AdapterStatus.PoweredOn)
            {
                BLEBridge.BleLogging("Adaptor not in powered on state!", "Try turning bluetooth on?", 3);
                // How can we adapt to this?
                if (adapter.CanControlAdapterState())
                    adapter.SetAdapterState(true);

                else if (adapter.CanOpenSettings())
                {
                    adapter.OpenSettings();
                    return false;
                }
            }

            foreach (var device in adapter.GetConnectedDevices())
            {
                if (matches(device))
                {
                    MWDevice mwdevice = new MWDevice(device);
                    bool success = await whenFound(mwdevice);
                    if (success)
                        return true;
                }
            }

            foreach (var device in adapter.GetPairedDevices())
            {
                if (matches(device))
                {
                    MWDevice mwdevice = new MWDevice(device);
                    bool success = await whenFound(mwdevice);
                    if (success)
                        return true;
                }
            }

            scan = CrossBleAdapter.Current.ScanForUniqueDevices()
                    .Where(device => matches(device))
                    .FirstAsync()
                    .Subscribe(async device =>
                    {
                        MWDevice mwdevice = new MWDevice(device);
                        bool success = await whenFound(mwdevice);
                        if (success)
                            StopScanning();
                    });

            return true;
        }

        public void StopScanning()
        {
            scan?.Dispose();
            scan = null;
        }
    }
}
