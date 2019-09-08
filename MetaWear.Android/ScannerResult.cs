using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MbientLab.MetaWear;

namespace MetaWear.Android
{
    internal class ScannerResult : IScannerResult
    {
        public ScannerResult(BluetoothDevice device)
        {
            Device = device;
        }

        public BluetoothDevice Device { get; internal set; }

        public ulong Address => ulong.Parse(Device.Address, NumberStyles.AllowHexSpecifier);

        public string Name => Device.Name;
    }
}