using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerBma255;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;
using System.Threading.Tasks;
using System;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class AccelerometerBma255 : AccelerometerBosch, IAccelerometerBma255 {
        public const byte IMPLEMENTATION = 0x3;
        private static readonly float[] FREQUENCIES = new float[] { 15.62f, 31.26f, 62.5f, 125f, 250f, 500f, 1000f, 2000f };

        private class Bma255FlatDataProducer : BoschFlatDataProducer, IBma255FlatDataProducer {
            private static readonly ushort[] HOLD_TIMES = new ushort[] { 0, 512, 1024, 2048 };
            internal Bma255FlatDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {

            }

            public override void Configure(ushort? holdTime = null, float? theta = null) {
                Configure((FlatHoldTime)Util.closestIndexUShort(HOLD_TIMES, holdTime ?? 512), theta);
            }

            public void Configure(FlatHoldTime? holdTime = null, float? theta = null) {
                Write((byte) (holdTime ?? FlatHoldTime._512ms), theta ?? 5.6889f);
            }
        }
        protected class Bma255MotionDataProducer : BoschMotionDataProducer {
            private byte[] InitialMotionConfig => new byte[] { 0x00, 0x14, 0x14 };

            internal Bma255MotionDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) :
                    base(dataTypeBase, bridge) {
            }

            public override void ConfigureAny(int? Count = null, float? Threshold = null) {
                ConfigureAnyInner(InitialMotionConfig, Count, Threshold);
            }

            public override void ConfigureNo(int? Duration = null, float? Threshold = null) {
                byte[] config = InitialMotionConfig;
                if (Duration.HasValue) {
                    config[0] &= 0x3;

                    if (Duration >= 1000 && Duration <= 16000) {
                        config[0] |= (byte)(((Duration - 1000) / 1000) << 2);
                    } else if (Duration >= 20000 && Duration <= 80000) {
                        config[0] |= (byte) ((((byte)(Duration - 20000) / 4000) << 2) | 0x40);
                    } else if (Duration >= 88000 && Duration <= 336000) {
                        config[0] |= (byte) ((((byte)(Duration - 88000) / 8000) << 2) | 0x80);
                    }
                }

                if (Threshold.HasValue) {
                    config[2] = (byte)(Threshold / BOSCH_NO_MOTION_THS_STEPS[(accelerometer as AccelerometerBma255).DataScaleIndex]);
                }

                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }

            public override void ConfigureSlow(byte? Count = null, float? Threshold = null) {
                ConfigureSlowInner(InitialMotionConfig, Count, Threshold);
            }
        }

        [DataMember] private readonly byte[] accDataConfig = new byte[] { 0x0b, 0x03 };

        private IBma255FlatDataProducer flatDetector;
        private TimedTask<byte[]> readConfigTask;

        protected override float DataScale {
            get {
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
        protected override int DataScaleIndex {
            get {
                switch (accDataConfig[1] & 0x0f) {
                    case 0x3:
                        return 0;
                    case 0x5:
                        return 1;
                    case 0x8:
                        return 2;
                    case 0xc:
                        return 3;
                    default:
                        throw new InvalidOperationException(string.Format("Invalid data range for bma255 accDataConfig[1]={0:X}", accDataConfig[1]));
                }
            }
        }

        protected override byte MaxOrientHys => 0x7;

        IBma255FlatDataProducer IAccelerometerBma255.Flat {
            get {
                if (flatDetector == null) {
                    flatDetector = new Bma255FlatDataProducer(flatDataType, bridge);
                }
                return flatDetector;
            }
        }
        public override IFlatDataProducer Flat => Flat;

        protected override byte[] InitialLowHighGConfig => new byte[] { 0x09, 0x30, 0x81, 0x0f, 0xc0 };

        protected override float LowHighGDurationStep => 2.0f;

        public override IMotionDataProducer Motion => throw new NotImplementedException();

        protected override void init() {
            base.init();

            readConfigTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)ACCELEROMETER, Util.setRead(DATA_CONFIG)), 
                response => readConfigTask.SetResult(response));
        }

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

        public async override Task PullConfigAsync() {
            byte[] response = await readConfigTask.Execute("Did not receive accelerometer config within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, Util.setRead(DATA_CONFIG) }));
            Array.Copy(response, 2, accDataConfig, 0, accDataConfig.Length);
        }
    }
}
