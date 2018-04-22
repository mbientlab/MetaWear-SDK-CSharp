using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerBmi160;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class AccelerometerBmi160 : AccelerometerBosch, IAccelerometerBmi160 {
        public const byte IMPLEMENTATION = 0x1;

        private const byte STEP_DETECTOR_INTERRUPT_ENABLE = 0x17,
            STEP_DETECTOR_CONFIG = 0x18,
            STEP_DETECTOR_INTERRUPT = 0x19,
            STEP_COUNTER_DATA = 0x1a,
            STEP_COUNTER_RESET = 0x1b;
        private static readonly float[] FREQUENCIES = new float[] { 0.078125f, 1.5625f, 3.125f, 6.25f, 12.5f, 25f, 50f, 100f, 200f, 400f, 800f, 1600f };
        private static void applyDetectorMode(byte[] config, StepDetectorMode mode) {
            switch (mode) {
                case StepDetectorMode.Normal:
                    config[0] = 0x15;
                    config[1] |= 0x3;
                    break;
                case StepDetectorMode.Sensitive:
                    config[0] = 0x2d;
                    break;
                case StepDetectorMode.Robust:
                    config[0] = 0x1d;
                    config[1] |= 0x7;
                    break;
            }
        }
        internal new static string createIdentifier(DataTypeBase dataType) {
            switch (Util.clearRead(dataType.eventConfig[1])) {
                case STEP_DETECTOR_INTERRUPT:
                    return "step-detector";
                case STEP_COUNTER_DATA:
                    return "step-counter";
            }

            return AccelerometerBosch.createIdentifier(dataType);
        }

        private class StepCounterDataProducer : ForcedDataProducer, IStepCounterDataProducer {
            private readonly byte[] config = new byte[] { 0x00, 0x08 };

            internal StepCounterDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {
            }

            public void Configure(StepDetectorMode Mode = StepDetectorMode.Normal) {
                applyDetectorMode(config, Mode);
                bridge.sendCommand(ACCELEROMETER, STEP_DETECTOR_CONFIG, config);
            }

            public void Reset() {
                bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, STEP_COUNTER_RESET });
            }
        }

        private class StepDetectorDataProducer : AsyncDataProducerV2, IStepDetectorDataProducer {
            private readonly byte[] config = new byte[] { 0x00, 0x00 };

            internal StepDetectorDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(STEP_DETECTOR_INTERRUPT_ENABLE, 0x1, dataTypeBase, bridge) {
            }

            public void Configure(StepDetectorMode Mode = StepDetectorMode.Normal) {
                applyDetectorMode(config, Mode);
                bridge.sendCommand(ACCELEROMETER, STEP_DETECTOR_CONFIG, config);
            }
        }

        private class Bmi160FlatDataProducer : BoschFlatDataProducer, IBmi160FlatDataProducer {
            private static readonly ushort[] HOLD_TIMES = new ushort[] { 0, 640, 1280, 2560 };
            internal Bmi160FlatDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {

            }

            public override void Configure(ushort? hold = null, float? theta = null) {
                Configure((FlatHoldTime)Util.ClosestIndex_ushort(HOLD_TIMES, hold ?? 640), theta);
            }

            public void Configure(FlatHoldTime? hold = null, float? theta = null) {
                Write((byte)(hold ?? FlatHoldTime._640ms), theta ?? 5.6889f);
            }
        }

        protected class Bmi160MotionDataProducer : BoschMotionDataProducer, IBmi160MotionDataProducer {
            private byte[] InitialMotionConfig => new byte[] { 0x00, 0x14, 0x14, 0x14 };

            internal Bmi160MotionDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) :
                    base(dataTypeBase, bridge) {
            }

            public override void ConfigureAny(int? count = null, float? threshold = null) {
                byte[] config = InitialMotionConfig;
                config[3] &= (~0x2) & 0xff;

                ConfigureAnyInner(config, count, threshold);
            }

            public override void ConfigureNo(int? duration = null, float? threshold = null) {
                byte[] config = InitialMotionConfig;
                config[3] |= 0x1;

                if (duration.HasValue) {
                    config[0] &= 0x3;

                    if (duration >= 1280 && duration <= 20480) {
                        config[0] |= (byte)(((byte)(duration / 1280f - 1)) << 2);
                    } else if (duration >= 25600 && duration <= 102400) {
                        config[0] |= (byte)((((byte)(duration / 5120f - 5)) << 2) | 0x40);
                    } else if (duration >= 112640 && duration <= 430080) {
                        config[0] |= (byte)((((byte)(duration / 10240f - 11)) << 2) | 0x80);
                    }
                }

                if (threshold.HasValue) {
                    config[2] = (byte)(threshold / BOSCH_NO_MOTION_THS_STEPS[(accelerometer as AccelerometerBmi160).DataScaleIndex]);
                }

                mask = 0x38;
                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }

            public void ConfigureSignificant(SkipTime? skip = null, ProofTime? proof = null) {
                byte[] config = InitialMotionConfig;
                config[3] |= 0x2;

                if (skip.HasValue) {
                    config[3] |= (byte)((int)skip << 2);
                }
                if (proof.HasValue) {
                    config[3] |= (byte)((int)proof << 4);
                }

                mask = 0x7;
                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }

            public override void ConfigureSlow(byte? count = null, float? threshold = null) {
                byte[] config = InitialMotionConfig;
                config[3] &= (~0x1) & 0xff;

                ConfigureSlowInner(config, count, threshold);
            }
        }

        [DataMember] private readonly byte[] accDataConfig = new byte[] { 0x28, 0x03 };
        [DataMember] private IntegralDataType stepCounterDataType, stepDetectorDataType;

        private IStepCounterDataProducer stepCounter;
        private IStepDetectorDataProducer stepDetector;
        private IBmi160FlatDataProducer flatDetector;
        private IBmi160MotionDataProducer motion;
        private TimedTask<byte[]> readConfigTask;

        public IStepCounterDataProducer StepCounter {
            get {
                if (stepCounter == null) {
                    stepCounter = new StepCounterDataProducer(stepCounterDataType, bridge);
                }
                return stepCounter;
            }
        }
        public IStepDetectorDataProducer StepDetector {
            get {
                if (stepDetector == null) {
                    stepDetector = new StepDetectorDataProducer(stepDetectorDataType, bridge);
                }
                return stepDetector;
            }
        }
        IBmi160FlatDataProducer IAccelerometerBmi160.Flat => Flat as IBmi160FlatDataProducer;
        public override IFlatDataProducer Flat {
            get {
                if (flatDetector == null) {
                    flatDetector = new Bmi160FlatDataProducer(flatDataType, bridge);
                }
                return flatDetector;
            }
        }

        IBmi160MotionDataProducer IAccelerometerBmi160.Motion => Motion as IBmi160MotionDataProducer;
        public override IMotionDataProducer Motion {
            get {
                if (motion == null) {
                    motion = new Bmi160MotionDataProducer(motionDataType, bridge);
                }
                return motion;
            }
        }

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
                        throw new InvalidOperationException(string.Format("Invalid data range for bmi160 accDataConfig[1]={0:X}", accDataConfig[1]));
                }
            }
        }

        protected override byte MaxOrientHys => 0xf;
        protected override byte[] InitialLowHighGConfig => new byte[] { 0x07, 0x30, 0x81, 0x0b, 0xc0 };
        protected override float LowHighGDurationStep => 2.5f;

        public override float Odr => FREQUENCIES[(accDataConfig[0] & 0xf) - 1];

        public AccelerometerBmi160(IModuleBoardBridge bridge) : base(bridge) {
            stepCounterDataType = new IntegralDataType(ACCELEROMETER, Util.setRead(STEP_COUNTER_DATA), new DataAttributes(new byte[] { 2 }, 1, 0, false));
            stepDetectorDataType = new IntegralDataType(ACCELEROMETER, STEP_DETECTOR_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false));
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            base.aggregateDataType(collection);

            collection.Add(stepCounterDataType);
            collection.Add(stepDetectorDataType);
        }

        protected override void init() {
            base.init();

            readConfigTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)ACCELEROMETER, Util.setRead(DATA_CONFIG)), 
                response => readConfigTask.SetResult(response));
        }

        public void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g, FilterMode filter = FilterMode.Normal) {
            accDataConfig[0] = (byte) (((int) odr + 1) | ((FREQUENCIES[(int)odr] < 12.5f) ? 0x80 : (int)filter << 4));
            accDataConfig[1] &= 0xf0;
            accDataConfig[1] |= RANGE_BIT_MASKS[(int) range];

            bridge.sendCommand(ACCELEROMETER, DATA_CONFIG, accDataConfig);
        }

        public override void Configure(float odr = 100f, float range = 2f) {
            Configure((OutputDataRate) Util.ClosestIndex_float(FREQUENCIES, odr), (DataRange) Util.ClosestIndex_float(RANGES, range));
        }

        public async override Task PullConfigAsync() {
            byte[] response = await readConfigTask.Execute("Did not receive accelerometer config within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, Util.setRead(DATA_CONFIG) }));
            Array.Copy(response, 2, accDataConfig, 0, accDataConfig.Length);
        }
    }
}
