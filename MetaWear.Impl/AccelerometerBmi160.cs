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
            switch (dataType.eventConfig[1]) {
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

            public override void Configure(ushort? Hold = null, float? Theta = null) {
                Configure((FlatHoldTime) Util.closestIndexUShort(HOLD_TIMES, Hold ?? 640), Theta);
            }

            public void Configure(FlatHoldTime? Hold = null, float? Theta = null) {
                Write((byte) (Hold ?? FlatHoldTime._640ms), Theta ?? 5.6889f);
            }
        }

        protected class Bmi160MotionDataProducer : BoschMotionDataProducer, IBmi160MotionDataProducer {
            private byte[] InitialMotionConfig => new byte[] { 0x00, 0x14, 0x14, 0x14 };

            internal Bmi160MotionDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) :
                    base(dataTypeBase, bridge) {
            }

            public override void ConfigureAny(int? Count = null, float? Threshold = null) {
                byte[] config = InitialMotionConfig;
                config[3] &= (~0x2) & 0xff;

                ConfigureAnyInner(config, Count, Threshold);
            }

            public override void ConfigureNo(int? Duration = null, float? Threshold = null) {
                byte[] config = InitialMotionConfig;
                config[3] |= 0x1;

                if (Duration.HasValue) {
                    config[0] &= 0x3;

                    if (Duration >= 1280 && Duration <= 20480) {
                        config[0] |= (byte) (((byte)(Duration / 1280f - 1)) << 2);
                    } else if (Duration >= 25600 && Duration <= 102400) {
                        config[0] |= (byte) ((((byte)(Duration / 5120f - 5)) << 2) | 0x40);
                    } else if (Duration >= 112640 && Duration <= 430080) {
                        config[0] |= (byte) ((((byte)(Duration / 10240f - 11)) << 2) | 0x80);
                    }
                }

                if (Threshold.HasValue) {
                    config[2] = (byte)(Threshold / BOSCH_NO_MOTION_THS_STEPS[(accelerometer as AccelerometerBmi160).DataScaleIndex]);
                }

                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }

            public void ConfigureSignificant(SkipTime? Skip = null, ProofTime? Proof = null) {
                byte[] config = InitialMotionConfig;
                config[3] |= 0x2;

                if (Skip.HasValue) {
                    config[3] |= (byte) ((int) Skip << 2);
                }
                if (Proof.HasValue) {
                    config[3] |= (byte)((int) Proof << 4);
                }

                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }

            public override void ConfigureSlow(byte? Count = null, float? Threshold = null) {
                byte[] config = InitialMotionConfig;
                config[3] &= (~0x1) & 0xff;

                ConfigureSlowInner(config, Count, Threshold);
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
        IBmi160FlatDataProducer IAccelerometerBmi160.Flat {
            get {
                if (flatDetector == null) {
                    flatDetector = new Bmi160FlatDataProducer(flatDataType, bridge);
                }
                return flatDetector;
            }
        }
        public override IFlatDataProducer Flat => Flat;

        IBmi160MotionDataProducer IAccelerometerBmi160.Motion {
            get {
                if (motion == null) {
                    motion = new Bmi160MotionDataProducer(motionDataType, bridge);
                }
                return motion;
            }
        }
        public override IMotionDataProducer Motion => Motion;

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

        public void Configure(OutputDataRate odr, DataRange range) {
            accDataConfig[0] &= 0xf0;
            accDataConfig[0] |= (byte)(odr + 1);

            accDataConfig[0] &= 0xf;
            accDataConfig[0] |= (byte) ((FREQUENCIES[(int)odr] < 12.5f) ? 0x80 : 0x20);

            accDataConfig[1] &= 0xf0;
            accDataConfig[1] |= RANGE_BIT_MASKS[(int) range];

            bridge.sendCommand(ACCELEROMETER, DATA_CONFIG, accDataConfig);
        }

        public override void Configure(float odr = 100f, float range = 2f) {
            Configure((OutputDataRate) Util.closestIndex(FREQUENCIES, odr), (DataRange) Util.closestIndex(RANGES, range));
        }

        public async override Task PullConfigAsync() {
            byte[] response = await readConfigTask.Execute("Did not receive accelerometer config within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, Util.setRead(DATA_CONFIG) }));
            Array.Copy(response, 2, accDataConfig, 0, accDataConfig.Length);
        }
    }
}
