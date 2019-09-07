using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MbientLab.MetaWear
{
    public interface IApplication
    {
        IScanner Scanner { get; }

        IMetaWearBoard GetMetaWearBoard(IScannerResult result);

        void RemoveMetaWearBoard(string address, bool dispose = false);
        void RemoveMetaWearBoard(uint address, bool dispose = false);

        Task ClearDeviceCacheAsync(IScannerResult device);
    }
}
