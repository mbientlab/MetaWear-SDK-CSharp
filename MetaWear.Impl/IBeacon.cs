using MbientLab.MetaWear.Peripheral;
using static MbientLab.MetaWear.Impl.Module;

using System;
using MbientLab.MetaWear.Peripheral.IBeacon;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class IBeacon : ModuleImplBase, IIBeacon {
        private const byte ENABLE = 0x1, AD_UUID = 0x2, MAJOR = 0x3, MINOR = 0x4,
                RX = 0x5, TX = 0x6, PERIOD = 0x7;

        private TaskCompletionSource<Configuration> readConfigTask;
        private Guid adUuid;
        private ushort major, minor, period;
        private sbyte rxPower, txPower;
        private Timer readConfigTimeout;

        public IBeacon(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            bridge.addRegisterResponseHandler(Tuple.Create((byte) IBEACON, Util.setRead(AD_UUID)), response => {
                Array.Reverse(response);
                Array.Reverse(response, 0, 4);
                Array.Reverse(response, 4, 2);
                Array.Reverse(response, 6, 2);

                adUuid = new Guid(response);

                bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(MAJOR) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(MAJOR)), response => {
                major = BitConverter.ToUInt16(response, 0);

                bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(MINOR) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(MINOR)), response => {
                minor = BitConverter.ToUInt16(response, 0);

                bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(RX) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(RX)), response => {
                rxPower = (sbyte) response[0];

                bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(TX) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(TX)), response => {
                txPower = (sbyte)response[0];

                bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(PERIOD) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)IBEACON, Util.setRead(PERIOD)), response => {
                readConfigTimeout.Dispose();

                period = BitConverter.ToUInt16(response, 0);
                readConfigTask.SetResult(new Configuration(adUuid, major, minor, period, rxPower, txPower));
                readConfigTask = null;
            });
        }

        public void Disable() {
            bridge.sendCommand(new byte[] { (byte)IBEACON, ENABLE, 0x0 });
        }

        public void Enable() {
            bridge.sendCommand(new byte[] { (byte)IBEACON, ENABLE, 0x1 });
        }

        public Task<Configuration> ReadConfigAsync() {
            readConfigTask = new TaskCompletionSource<Configuration>();
            readConfigTimeout = new Timer(e => readConfigTask.SetException(new TimeoutException("Reading ibeacon config timedout")), null, 7 * 250, Timeout.Infinite);
            bridge.sendCommand(new byte[] { (byte)IBEACON, Util.setRead(AD_UUID) });

            return readConfigTask.Task;
        }

        public void SetMajor(IDataToken major) {
            bridge.sendCommand(new byte[] { (byte)IBEACON, MAJOR, 0x0, 0x0 }, 0, major);
        }

        public void SetMinor(IDataToken minor) {
            bridge.sendCommand(new byte[] { (byte)IBEACON, MINOR, 0x0, 0x0 }, 0, minor);
        }

        public void Configure(Guid? uuid = default(Guid?), ushort? major = default(ushort?), ushort? minor = default(ushort?), 
                sbyte? txPower = default(sbyte?), sbyte? rxPower = default(sbyte?), 
                ushort? period = default(ushort?)) {
            if (uuid != null) {
                byte[] guidBytes = uuid.Value.ToByteArray();

                // Implementation taken from SO: http://stackoverflow.com/a/16722909
                Array.Reverse(guidBytes, 0, 4);
                Array.Reverse(guidBytes, 4, 2);
                Array.Reverse(guidBytes, 6, 2);
                Array.Reverse(guidBytes);

                bridge.sendCommand(IBEACON, AD_UUID, guidBytes);
            }

            if (major != null) {
                bridge.sendCommand(IBEACON, MAJOR, Util.ushortToBytesLe(major.Value));
            }

            if (minor != null) {
                bridge.sendCommand(IBEACON, MINOR, Util.ushortToBytesLe(minor.Value));
            }

            if (rxPower != null) {
                bridge.sendCommand(new byte[] { (byte)IBEACON, RX, (byte)rxPower.Value });
            }

            if (txPower != null) {
                bridge.sendCommand(new byte[] { (byte)IBEACON, TX, (byte)txPower.Value });
            }

            if (period != null) {
                bridge.sendCommand(IBEACON, PERIOD, Util.ushortToBytesLe(period.Value));
            }
        }
    }
}
