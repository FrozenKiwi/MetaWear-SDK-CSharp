using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(DataTypeBase))]
    [DataContract]
    abstract class DeviceDataConsumer {
        [DataMember] internal readonly DataTypeBase source;
        [DataMember] internal string name;
        internal Action<IData> handler;

        public DeviceDataConsumer(DataTypeBase source) {
            this.source = source;
        }

        public DeviceDataConsumer(DataTypeBase source, Action<IData> handler) : this(source) {
            this.handler = handler;
        }

        public void call(IData data) {
            handler?.Invoke(data);
        }

        public abstract Task enableStream(IModuleBoardBridge bridge);
        public abstract Task disableStream(IModuleBoardBridge bridge);
        public abstract Task addDataHandler(IModuleBoardBridge bridge);
    }
}
