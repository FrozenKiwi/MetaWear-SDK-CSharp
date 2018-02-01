﻿using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    class AsyncDataProducer : DataProducer, IAsyncDataProducer {
        protected readonly byte enableRegister;

        internal AsyncDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : 
            this(dataTypeBase.eventConfig[1], dataTypeBase, bridge) { }

        internal AsyncDataProducer(byte register, DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {
            enableRegister = register;
        }

        public virtual Task Start() {
            return bridge.sendCommand(new byte[] { dataTypeBase.eventConfig[0], enableRegister, 0x01 });
        }

        public virtual Task Stop() {
            return bridge.sendCommand(new byte[] { dataTypeBase.eventConfig[0], enableRegister, 0x00 });
        }
    }
}
