using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerBma255;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class AccelerometerBma255 : AccelerometerBosch, IAccelerometerBma255 {
        public const byte IMPLEMENTATION = 0x3;
        private static readonly float[] FREQUENCIES = new float[] { 15.62f, 31.26f, 62.5f, 125f, 250f, 500f, 1000f, 2000f };

        [DataMember] private readonly byte[] accDataConfig = new byte[] { 0x0b, 0x03 };

        public AccelerometerBma255(IModuleBoardBridge bridge) : base(bridge) {
        }

        public void Configure(OutputDataRate odr, DataRange range) {
            accDataConfig[0] &= 0xe0;
            accDataConfig[0] |= (byte) (odr + 8);

            accDataConfig[1] &= 0xf0;
            accDataConfig[1] |= RANGE_BIT_MASKS[(int) range];

            bridge.sendCommand(ACCELEROMETER, DATA_CONFIG, accDataConfig);
        }

        public override void Configure(float odr = 100f, float range = 2f) {
            Configure((OutputDataRate)Util.closestIndex(FREQUENCIES, odr), (DataRange)Util.closestIndex(RANGES, range));
        }

        protected override float GetDataScale() {
            switch (accDataConfig[1] & 0x0f) {
                case 0x3:
                    return 16384f;
                case 0x5:
                    return 8192f;
                case 0x8:
                    return 4096f;
                case 0xc:
                    return 2048f;
                default:
                    return 1f;
            }
        }
    }
}
