using MbientLab.MetaWear.Peripheral;
using static MbientLab.MetaWear.Impl.Module;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Haptic : ModuleImplBase, IHaptic {
        private const byte PULSE = 0x1;
        private const byte BUZZER_DUTY_CYCLE = 127;

        public Haptic(IModuleBoardBridge bridge) : base(bridge) {
        }

        public Task StartBuzzer(ushort pulseWidth) {
            byte[] parameters = new byte[] { BUZZER_DUTY_CYCLE, 0, 0, 1 };
            Array.Copy(Util.ushortToBytesLe(pulseWidth), 0, parameters, 1, 2);

            return bridge.sendCommand(HAPTIC, PULSE, parameters);
        }

        public Task StartMotor(ushort pulseWidth, float dutyCycle = 100f) {
            byte converted = (byte)((dutyCycle / 100) * 248);
            byte[] parameters = new byte[] { converted, 0, 0, 0 };
            Array.Copy(Util.ushortToBytesLe(pulseWidth), 0, parameters, 1, 2);

            return bridge.sendCommand(HAPTIC, PULSE, parameters);
        }
    }
}
