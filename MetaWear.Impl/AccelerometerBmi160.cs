using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerBmi160;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class AccelerometerBmi160 : AccelerometerBosch, IAccelerometerBmi160 {
        public const byte IMPLEMENTATION = 0x1;
        
        private static readonly float[] FREQUENCIES = new float[] { 0.078125f, 1.5625f, 3.125f, 6.25f, 12.5f, 25f, 50f, 100f, 200f, 400f, 800f, 1600f };

        [DataMember] private readonly byte[] accDataConfig = new byte[] { 0x28, 0x03 };

        public AccelerometerBmi160(IModuleBoardBridge bridge) : base(bridge) { }

        public void Configure(OutputDataRate odr, DataRange range) {
            accDataConfig[0] &= 0xf0;
            accDataConfig[0] |= (byte)(odr + 1);

            accDataConfig[0] &= 0xf;
            if (FREQUENCIES[(int) odr] < 12.5f) {
                accDataConfig[0] |= 0x80;
            } else {
                accDataConfig[0] |= 0x20;
            }

            accDataConfig[1] &= 0xf0;
            accDataConfig[1] |= RANGE_BIT_MASKS[(int) range];

            bridge.sendCommand(ACCELEROMETER, DATA_CONFIG, accDataConfig);
        }

        public override void Configure(float odr = 100f, float range = 2f) {
            Configure((OutputDataRate) Util.closestIndex(FREQUENCIES, odr), (DataRange) Util.closestIndex(RANGES, range));
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
