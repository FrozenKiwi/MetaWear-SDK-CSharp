using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.NeoPixel;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class NeoPixel : ModuleImplBase, INeoPixel {
        private const byte INITIALIZE = 1,
            HOLD = 2,
            CLEAR = 3, SET_COLOR = 4,
            ROTATE = 5,
            FREE = 6;

        [DataContract]
        private class Strand : SerializableType, IStrand {
            private int nLeds;
            private byte id;

            public Strand(byte id, int nLeds, IModuleBoardBridge bridge) : base(bridge) {
                this.nLeds = nLeds;
                this.id = id;
            }

            public int NLeds => nLeds;

            public Task Clear(byte start, byte end) {
                return bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, CLEAR, id, start, end });
            }

            public async Task Free() {
                await bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, FREE, id });
                (bridge.GetModule<INeoPixel>() as NeoPixel).strands[id] = null;
            }

            public Task Hold() {
                return bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, HOLD, id, 0x1 });
            }

            public Task Release() {
                return bridge.sendCommand(new byte[] { (byte)NEO_PIXEL, HOLD, id, 0x0 });
            }

            public Task Rotate(RotationDirection direction, ushort period, byte repetitions = 255) {
                return bridge.sendCommand(new byte[] { (byte)NEO_PIXEL, ROTATE, id, (byte)direction, repetitions,
                        (byte)(period & 0xff), (byte)(period >> 8 & 0xff)});
            }

            public Task SetRgb(byte index, byte red, byte green, byte blue) {
                return bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, SET_COLOR, id, index, red, green, blue });
            }

            public Task StopRotation() {
                return bridge.sendCommand(new byte[] { (byte)NEO_PIXEL, ROTATE, id, 0x0, 0x0, 0x0, 0x0 });
            }
        }

        [DataMember] private Strand[] strands = new Strand[3];

        public NeoPixel(IModuleBoardBridge bridge) : base(bridge) {
        }

        public async Task<IStrand> InitializeStrand(byte id, ColorOrdering ordering, StrandSpeed speed, byte gpioPin, byte nLeds) {
            strands[id] = new Strand(id, nLeds, bridge);
            await bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, INITIALIZE, id, (byte)((byte) speed << 2 | (byte) ordering), gpioPin, nLeds });
            return strands[id];
        }

        public IStrand LookupStrand(byte id) {
            return strands[id];
        }
    }
}
