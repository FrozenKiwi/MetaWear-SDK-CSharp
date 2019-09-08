using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Bluetooth;
using MbientLab.MetaWear;
using Android.Bluetooth.LE;
using System.Threading;

namespace MetaWear.Android
{
    class Scanner : IScanner
    {


        private HashSet<string> seenDevices = new HashSet<string>();
        private Dictionary<string, BluetoothDevice> pairedDevices = new Dictionary<string, BluetoothDevice>();
        private ScanConfig config = new ScanConfig();
        private readonly BluetoothAdapter mBluetoothAdapter;
        private readonly BluetoothLeScanner btleWatcher;

        private Timer timer;

        public event EventHandler<IScannerResult> OnDeviceFound;

        public Scanner()
        {
            mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            btleWatcher = mBluetoothAdapter.BluetoothLeScanner;


            //bluetoothLeScanner.StartScan(Received += async (w, btAdv) =>
            //{

            //};
        }

        public void OnSeenDevice(ScanResult result)
        {
            var address = result.Device.Address;
            if (!seenDevices.Contains(address) &&
                config.ServiceUuids
                    .Aggregate(true, (acc, e) => {
                        return acc & result.ScanRecord.ServiceData.ContainsKey(ParcelUuid.FromString(e.ToString()));
                    }))
            {
                seenDevices.Add(address);
                var device = result.Device;
                if (device != null)
                {
                    pairedDevices.Add(device.Name, device);
                    OnDeviceFound?.Invoke(this, new ScannerResult(device));
                }
            }
        }




        public void RefreshDevices()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            StopScanning();

            var connected = pairedDevices.Where(e => e.Value.BondState == Bond.Bonded);

            seenDevices.Clear();
            pairedDevices.Clear();

            foreach (var it in connected)
            {
                seenDevices.Add(it.Value.Address);
                pairedDevices.Add(it.Key, it.Value);
            }

            btleWatcher.StartScan(new Callback(this));
            timer = new Timer(e => StopScanning(), null, config.Duration, Timeout.Infinite);
        }

        public void StopScanning()
        {
            btleWatcher.StopScan(new Callback(this));
        }

        internal class Callback : ScanCallback
        {
            private Scanner scanner;

            internal Callback(Scanner scanner)
            {
                this.scanner = scanner;
            }
            override public void OnBatchScanResults(IList<ScanResult> results)
            {

            }
            override public void OnScanFailed(ScanFailure errorCode)
            {
                throw new Exception("Scanning Faild: " + errorCode);
            }
            override public void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
                scanner.OnDeviceFound.Invoke(scanner, new ScannerResult(result.Device));
            }
        }
    }
}