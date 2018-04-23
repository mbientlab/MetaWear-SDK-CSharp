using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerMma8452q;
using System.Threading.Tasks;
using System.Collections.Generic;
using MbientLab.MetaWear.Data;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(Mma8452QCartesianFloatData))]
    [DataContract]
    class AccelerometerMma8452q : ModuleImplBase, IAccelerometerMma8452q {
        internal static string createIdentifier(DataTypeBase dataType) {
            switch (dataType.eventConfig[1]) {
                case DATA_VALUE:
                    return dataType.attributes.length() > 2 ? "acceleration" : string.Format("acceleration[{0}]", (dataType.attributes.offset >> 1));
                case ORIENTATION_VALUE:
                    return "orientation";
                /*
        case SHAKE_STATUS:
            return "mma8452q-shake";
        case PULSE_STATUS:
            return "mma8452q-tap";
        case MOVEMENT_VALUE:
            return "mma8452q-movement";
            */
                case PACKED_ACC_DATA:
                    // packed data is handled exactly the same as unpacked so give them the same identifier
                    return "acceleration";
                default:
                    return null;
            }
        }

        public const byte IMPLEMENTATION = 0, PACKED_ACC_REVISION = 1;
        private const byte GLOBAL_ENABLE = 1,
            DATA_ENABLE = 2, DATA_CONFIG = 3, DATA_VALUE = 4,
            ORIENTATION_ENABLE = 8, ORIENTATION_CONFIG = 9, ORIENTATION_VALUE = 0xa,
            PACKED_ACC_DATA = 0x12;

        private static readonly float[] FREQUENCIES = new float[] { 800f, 400f, 200f, 100f, 50f, 12.5f, 6.25f, 1.56f },
            RANGES = new float[] { 2f, 4f, 8f };
        private static readonly float[][] motionCountSteps = {
            new float[] {1.25f, 2.5f, 5, 10, 20, 20, 20, 20},
            new float[] {1.25f, 2.5f, 5, 10, 20, 80, 80, 80},
            new float[] {1.25f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f},
            new float[] {1.25f, 2.5f, 5, 10, 20, 80, 160, 160}
        }, orientationSteps = motionCountSteps;
        private static readonly float[][][] OS_CUTOFF_FREQS = {
            new float[][] {
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {8f, 4f, 2f, 1f},
                new float[] {4f, 2f, 1f, 0.5f},
                new float[] {2f, 1f, 0.5f, 0.25f},
                new float[] {2f, 1f, 0.5f, 0.25f},
                new float[] {2f, 1f, 0.5f, 0.25f},
                new float[] {2f, 1f, 0.5f, 0.25f}
            },
            new float[][] {
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {8f, 4f, 2f, 1f},
                new float[] {4f, 2f, 1f, 0.5f},
                new float[] {2f, 1f, 0.5f, 0.25f},
                new float[] {0.5f, 0.25f, 0.125f, 0.063f},
                new float[] {0.5f, 0.25f, 0.125f, 0.063f},
                new float[] {0.5f, 0.25f, 0.125f, 0.063f}
            },
            new float[][] {
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f},
                new float[] {16f, 8f, 4f, 2f}
            },
            new float[][] {
                new float[] {16f, 8f, 4f, 2f},
                new float[] {8f, 4f, 2f, 1f},
                new float[] {4f, 2f, 1f, 0.5f},
                new float[] {2f, 1f, 0.5f, 0.25f},
                new float[] {1f, 0.5f, 0.25f, 0.125f},
                new float[] {0.25f, 0.125f, 0.063f, 0.031f},
                new float[] {0.25f, 0.125f, 0.063f, 0.031f},
                new float[] {0.25f, 0.125f, 0.063f, 0.031f}
            }
        };

    [DataContract]
        private class Mma8452QCartesianFloatData : FloatVectorDataType {
            private Mma8452QCartesianFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            internal Mma8452QCartesianFloatData(byte register, byte copies) : 
                base(ACCELEROMETER, register, new DataAttributes(new byte[] { 2, 2, 2 }, copies, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new Mma8452QCartesianFloatData(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AccelerationData(bridge, this, timestamp, data);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return 1000f;
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 4, true))
                };
            }
        }

        [DataContract]
        private class Mma8452qOrientationDataType : DataTypeBase {
            private class OrientationData : DataBase {
                internal OrientationData(DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                        base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(SensorOrientation) };

                public override T Value<T>() {
                    var type = typeof(T);

                    if (type == typeof(SensorOrientation)) {
                        int offset = (bytes[0] & 0x06) >> 1;
                        return (T)Convert.ChangeType((SensorOrientation) (4 * (bytes[0] & 0x01) + ((offset == 2 || offset == 3) ? offset ^ 0x1 : offset)), type);
                    }

                    return base.Value<T>();
                }
            }

            internal Mma8452qOrientationDataType() : base(ACCELEROMETER, ORIENTATION_VALUE, new DataAttributes(new byte[] { 1 }, 1, 0, false)) {
            }

            internal Mma8452qOrientationDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                    base(input, module, register, id, attributes) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new Mma8452qOrientationDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new OrientationData(this, bridge, timestamp, data);
            }
        }
        private class OrientationDataProducer : AsyncDataProducer, IOrientationDataProducer {
            private int delay = 100;

            internal OrientationDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(ORIENTATION_ENABLE, dataTypeBase, bridge) {
            }

            public void Configure(int delay = 100) {
                this.delay = delay;
            }

            public override void Start() {
                AccelerometerMma8452q acc = bridge.GetModule<IAccelerometerMma8452q>() as AccelerometerMma8452q;
                bridge.sendCommand(ACCELEROMETER, ORIENTATION_CONFIG, new byte[] { 0x00, 0xc0, (byte)(delay / orientationSteps[acc.PwMode][acc.Odr]), 0x44, 0x84 });

                base.Start();
            }

            public override void Stop() {
                base.Stop();

                bridge.sendCommand(ACCELEROMETER, ORIENTATION_CONFIG, new byte[] { 0x00, 0x80, 0x00, 0x44, 0x84 });
            }
        }

        [DataMember] private readonly byte[] dataSettings = new byte[] { 0x00, 0x00, 0x18, 0x00, 0x00 };
        [DataMember] private Mma8452QCartesianFloatData accDataType, packedAccDataType;
        [DataMember] private Mma8452qOrientationDataType orientationDataType;

        private IAsyncDataProducer acceleration = null, packedAcceleration = null;
        private IOrientationDataProducer orientation = null;
        private TimedTask<byte[]> readConfigTask;


        float IAccelerometer.Odr => FREQUENCIES[Odr];
        public float Range => RANGES[dataSettings[0]];

        private int PwMode => dataSettings[3] & 0x3;
        private int Odr => (dataSettings[2] & ~0xc7) >> 3;
        private int PulseLpfEn => (dataSettings[1] & ~0x10) >> 4;

        public AccelerometerMma8452q(IModuleBoardBridge bridge) : base(bridge) {
            accDataType = new Mma8452QCartesianFloatData(DATA_VALUE, 1);
            packedAccDataType = new Mma8452QCartesianFloatData(PACKED_ACC_DATA, 3);
            orientationDataType = new Mma8452qOrientationDataType();
        }

        public IAsyncDataProducer Acceleration {
            get {
                if (acceleration == null) {
                    acceleration = new AsyncDataProducer(DATA_ENABLE, accDataType, bridge);
                }
                return acceleration;
            }
        }
        public IAsyncDataProducer PackedAcceleration {
            get {
                if (bridge.lookupModuleInfo(ACCELEROMETER).revision >= PACKED_ACC_REVISION) {
                    if (packedAcceleration == null) {
                        packedAcceleration = new AsyncDataProducer(DATA_ENABLE, packedAccDataType, bridge);
                    }
                    return packedAcceleration;
                }
                return null;
            }
        }

        public IOrientationDataProducer Orientation {
            get {
                if (orientation == null) {
                    orientation = new OrientationDataProducer(orientationDataType, bridge);
                }
                return orientation;
            }
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(accDataType);
            collection.Add(packedAccDataType);
            collection.Add(orientationDataType);
        }

        protected override void init() {
            readConfigTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)ACCELEROMETER, Util.setRead(DATA_CONFIG)), 
                response => readConfigTask.SetResult(response));
        }

        public void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g, float? highPassCutoff = null, 
                Oversampling oversample = Oversampling.Normal) {
            for(int i = 0; i < dataSettings.Length; i++) {
                dataSettings[i] = 0;
            }

            dataSettings[3] |= (byte)oversample;
            dataSettings[2] |= (byte)((byte)odr << 3);
            dataSettings[0] |= (byte)range;

            if (highPassCutoff != null) {
                dataSettings[1] |= (byte) (Util.ClosestIndex_float(OS_CUTOFF_FREQS[(int)oversample][(int)odr], highPassCutoff.Value) & 0x3);
                dataSettings[0] |= 0x10;
            }

            bridge.sendCommand(ACCELEROMETER, DATA_CONFIG, dataSettings);
        }

        public void Configure(float odr = 100f, float range = 2f) {
            Configure((OutputDataRate) Util.ClosestIndex_float(FREQUENCIES, odr), (DataRange) Util.ClosestIndex_float(RANGES, range));
        }

        public void Start() {
            bridge.sendCommand(new byte[] { (byte) ACCELEROMETER, GLOBAL_ENABLE, 0x1 });
        }
        public void Stop() {
            bridge.sendCommand(new byte[] { (byte) ACCELEROMETER, GLOBAL_ENABLE, 0x0 });
        }

        public async Task PullConfigAsync() {
            byte[] response = await readConfigTask.Execute("Did not receive accelerometer config within {0}ms", bridge.TimeForResponse, 
                () => bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, Util.setRead(DATA_CONFIG) }));
            Array.Copy(response, 2, dataSettings, 0, dataSettings.Length);
        }
    }
}
