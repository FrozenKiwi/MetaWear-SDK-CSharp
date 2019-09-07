using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MbientLab.MetaWear;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace MetaWear.UWP
{
    class Scanner : IScanner
    {

        private BluetoothLEAdvertisementWatcher btleWatcher;
        private HashSet<ulong> seenDevices = new HashSet<ulong>();
        private Dictionary<string, BluetoothLEDevice> pairedDevices = new Dictionary<string, BluetoothLEDevice>();
        private ScanConfig config = new ScanConfig();
        private Timer timer;

        public event EventHandler<IScannerResult> OnDeviceFound;

        public Scanner()
        {
            btleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            btleWatcher.Received += async (w, btAdv) =>
            {
                if (!seenDevices.Contains(btAdv.BluetoothAddress) &&
                    config.ServiceUuids.Aggregate(true, (acc, e) => acc & btAdv.Advertisement.ServiceUuids.Contains(e)))
                {
                    seenDevices.Add(btAdv.BluetoothAddress);
                    var device = await BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress);
                    if (device != null)
                    {
                        pairedDevices.Add(device.Name, device);
                        OnDeviceFound?.Invoke(this, new ScannerResult(device));
                    }
                }
            };
        }



        public void RefreshDevices()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            btleWatcher.Stop();

            var connected = pairedDevices.Where(e => e.Value.ConnectionStatus == BluetoothConnectionStatus.Connected);

            seenDevices.Clear();
            pairedDevices.Clear();

            foreach (var it in connected)
            {
                seenDevices.Add(it.Value.BluetoothAddress);
                pairedDevices.Add(it.Key, it.Value);
            }

            btleWatcher.Start();
            timer = new Timer(e => btleWatcher.Stop(), null, config.Duration, Timeout.Infinite);
        }

        public void StopScanning()
        {
            btleWatcher.Stop();
        }
    }
}
