using System;
using System.Collections.Generic;
using System.Text;

namespace MbientLab.MetaWear
{
    public interface IScannerResult
    {
        string Name { get; }

        ulong Address { get; }
    }
}
