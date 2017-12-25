using Plugin.BluetoothLE;
using System;
using System.Collections.Generic;
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

        public void StartScanning(Func<MWDevice, Task<bool>> whenFound)
        {
            StopScanning();
            scan = CrossBleAdapter.Current.Scan().Subscribe(async scanResult =>
            {
                var name = scanResult.Device.Name;
                var devGuid = scanResult.Device.Uuid;
                if (!seenDevices.ContainsKey(devGuid))
                {
                    // TODO: Verify this is actually a MetaWear device
                    var uuids = scanResult.AdvertisementData.ServiceUuids;
                    foreach (Guid uuid in uuids)
                    {
                        var st = uuid.ToString();
                    }
                    var s = scanResult.AdvertisementData.ToString();
                    //var serviceGuid = config.ServiceUuids.First();
                    //var service = await scanResult.Device.GetKnownService(serviceGuid).FirstOrDefaultAsync();
                    if (scanResult.Device.Name == "MetaWear")
                    {
                        StopScanning();
                        MWDevice mwdevice = new MWDevice(scanResult.Device);
                        seenDevices.Add(devGuid, mwdevice);
                        bool success = await whenFound(mwdevice);
                        if (!success)
                            StartScanning(whenFound);

                    }
                }
            });
        }

        public void StopScanning()
        {
            scan?.Dispose();
            scan = null;
        }
    }
}
