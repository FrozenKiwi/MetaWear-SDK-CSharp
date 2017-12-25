using System;
using System.Collections.Generic;
using System.Text;
using Plugin.BluetoothLE;

namespace MetaWear.NetStandard
{
    // Thin wrapper to underlying BLE device
    // Used during scanning when MetaWearDevice is too heavy
    public class MWDevice
    {
        internal IDevice device;

        public string Name => device.Name;

        public bool Connected => device.Status == ConnectionStatus.Connected;

        public Guid Uuid => device.Uuid;

        public MWDevice(IDevice device)
        {
            this.device = device;
        }
    }
}
