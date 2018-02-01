using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ForcedDataProducer : DataProducer, IForcedDataProducer {
        internal ForcedDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {
        }

        public Task Read() {
            return dataTypeBase.read(bridge);
        }
    }
}
