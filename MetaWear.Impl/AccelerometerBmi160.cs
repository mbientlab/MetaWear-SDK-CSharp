using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerBmi160;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

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

        private class StepCounterDataProducer : ForcedDataProducer, IStepCounterDataProducer {
            private readonly byte[] config = new byte[] { 0x00, 0x08 };

            internal StepCounterDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {
            }

            public void Configure(StepDetectorMode mode = StepDetectorMode.Normal) {
                applyDetectorMode(config, mode);
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

            public void Configure(StepDetectorMode mode = StepDetectorMode.Normal) {
                applyDetectorMode(config, mode);
                bridge.sendCommand(ACCELEROMETER, STEP_DETECTOR_CONFIG, config);
            }
        }

        [DataMember] private readonly byte[] accDataConfig = new byte[] { 0x28, 0x03 };
        [DataMember] private IntegralDataType stepCounterDataType, stepDetectorDataType;

        protected IStepCounterDataProducer stepCounter;
        protected IStepDetectorDataProducer stepDetector;

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

        public AccelerometerBmi160(IModuleBoardBridge bridge) : base(bridge) {
            stepCounterDataType = new IntegralDataType(ACCELEROMETER, Util.setRead(STEP_COUNTER_DATA), new DataAttributes(new byte[] { 2 }, 1, 0, false));
            stepDetectorDataType = new IntegralDataType(ACCELEROMETER, STEP_DETECTOR_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false));
        }

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
