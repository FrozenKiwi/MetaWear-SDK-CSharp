﻿using MbientLab.MetaWear.Peripheral;
using static MbientLab.MetaWear.Impl.Module;

using System;
using MbientLab.MetaWear.Peripheral.IBeacon;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class IBeacon : ModuleImplBase, IIBeacon {
        private const byte ENABLE = 0x1, AD_UUID = 0x2, MAJOR = 0x3, MINOR = 0x4,
                RX = 0x5, TX = 0x6, PERIOD = 0x7;

        private TimedTask<byte[]> readConfigTask;

        public IBeacon(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            readConfigTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte) IBEACON, Util.setRead(AD_UUID)), response => readConfigTask.SetResult(response));
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(MAJOR)), response => readConfigTask.SetResult(response));
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(MINOR)), response => readConfigTask.SetResult(response));
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(RX)), response => readConfigTask.SetResult(response));
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(TX)), response => readConfigTask.SetResult(response));
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(PERIOD)), response => readConfigTask.SetResult(response));
        }

        public Task Disable() {
            return bridge.sendCommand(new byte[] { (byte)IBEACON, ENABLE, 0x0 });
        }

        public Task Enable() {
            return bridge.sendCommand(new byte[] { (byte)IBEACON, ENABLE, 0x1 });
        }

        public async Task<Configuration> ReadConfigAsync() {
            var response = await readConfigTask.Execute("Did not receive ibeacon ad UUID within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(AD_UUID) }));

            byte[] copy = new byte[response.Length - 2];
            Array.Copy(response, 2, copy, 0, copy.Length);
            Array.Reverse(copy);
            Array.Reverse(copy, 0, 4);
            Array.Reverse(copy, 4, 2);
            Array.Reverse(copy, 6, 2);
            var adUuid = new Guid(copy);

            response = await readConfigTask.Execute("Did not receive ibeacon major ID within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(MAJOR) }));
            var major = BitConverter.ToUInt16(response, 2);

            response = await readConfigTask.Execute("Did not receive ibeacon minor ID within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(MINOR) }));
            var minor = BitConverter.ToUInt16(response, 2);

            response = await readConfigTask.Execute("Did not receive ibeacon rx power within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(RX) }));
            var rxPower = (sbyte)response[2];

            response = await readConfigTask.Execute("Did not receive ibeacon tx power within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(TX) }));
            var txPower = (sbyte)response[2];

            response = await readConfigTask.Execute("Did not receive ibeacon ad period within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(PERIOD) }));
            var period = BitConverter.ToUInt16(response, 2);

            return new Configuration(adUuid, major, minor, period, rxPower, txPower);
        }

        public Task SetMajor(IDataToken major) {
            return bridge.sendCommand(new byte[] { (byte)IBEACON, MAJOR, 0x0, 0x0 }, 0, major);
        }

        public Task SetMinor(IDataToken minor) {
            return bridge.sendCommand(new byte[] { (byte)IBEACON, MINOR, 0x0, 0x0 }, 0, minor);
        }

        public async Task Configure(Guid? uuid = default(Guid?), ushort? major = default(ushort?), ushort? minor = default(ushort?), 
                sbyte? txPower = default(sbyte?), sbyte? rxPower = default(sbyte?), 
                ushort? period = default(ushort?)) {
            if (uuid != null) {
                byte[] guidBytes = uuid.Value.ToByteArray();

                // Implementation taken from SO: http://stackoverflow.com/a/16722909
                Array.Reverse(guidBytes, 0, 4);
                Array.Reverse(guidBytes, 4, 2);
                Array.Reverse(guidBytes, 6, 2);
                Array.Reverse(guidBytes);

                await bridge.sendCommand(IBEACON, AD_UUID, guidBytes);
            }

            if (major != null) {
                await bridge.sendCommand(IBEACON, MAJOR, Util.ushortToBytesLe(major.Value));
            }

            if (minor != null) {
                await bridge.sendCommand(IBEACON, MINOR, Util.ushortToBytesLe(minor.Value));
            }

            if (rxPower != null) {
                await bridge.sendCommand(new byte[] { (byte)IBEACON, RX, (byte)rxPower.Value });
            }

            if (txPower != null) {
                await bridge.sendCommand(new byte[] { (byte)IBEACON, TX, (byte)txPower.Value });
            }

            if (period != null) {
                await bridge.sendCommand(IBEACON, PERIOD, Util.ushortToBytesLe(period.Value));
            }
        }
    }
}
