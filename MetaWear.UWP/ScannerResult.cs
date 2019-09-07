using MbientLab.MetaWear;
using Windows.Devices.Bluetooth;

namespace MetaWear.UWP
{
    internal class ScannerResult : IScannerResult
    {
        public ScannerResult(BluetoothLEDevice device)
        {
            Device = device;
        }

        public BluetoothLEDevice Device { get; internal set; }

        public ulong Address => Device.BluetoothAddress;

        public string Name => Device.Name;
    }
}