using System;
using System.Collections.Generic;
using System.Text;

namespace MbientLab.MetaWear
{
    public interface IScanner
    {
        void RefreshDevices();

        void StopScanning();

        event EventHandler<IScannerResult> OnDeviceFound;
    }

    public class ScanConfig
    {
        public int Duration { get; }
        public List<Guid> ServiceUuids { get; }

        public ScanConfig(int duration = 10000, List<Guid> serviceUuids = null)
        {
            Duration = duration;
            ServiceUuids = serviceUuids ?? new List<Guid>(new Guid[] { Constants.METAWEAR_GATT_SERVICE });
        }
    }
}
