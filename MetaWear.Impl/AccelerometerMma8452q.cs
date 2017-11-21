using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.AccelerometerMma8452q;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class AccelerometerMma8452q : ModuleImplBase, IAccelerometerMma8452q {
        internal static string createIdentifier(DataTypeBase dataType) {
            switch (dataType.eventConfig[1]) {
                case DATA_VALUE:
                    return dataType.attributes.length() > 2 ? "acceleration" : string.Format("acceleration[{0}]", (dataType.attributes.offset >> 1));
                /*
            case ORIENTATION_VALUE:
                return "orientation";
            case SHAKE_STATUS:
                return "mma8452q-shake";
            case PULSE_STATUS:
                return "mma8452q-tap";
            case MOVEMENT_VALUE:
                return "mma8452q-movement";
                */
                default:
                    return null;
            }
        }

        public const byte IMPLEMENTATION = 0, PACKED_ACC_REVISION = 1;
        private const byte GLOBAL_ENABLE = 1,
            DATA_ENABLE = 2, DATA_CONFIG = 3, DATA_VALUE = 4,
            PACKED_ACC_DATA = 0x12;

        private static readonly float[] FREQUENCIES = new float[] { 800f, 400f, 200f, 100f, 50f, 12.5f, 6.25f, 1.56f },
            RANGES = new float[] { 2f, 4f, 8f };

        [DataContract]
        private class Mma8452QCartesianFloatData : FloatVectorDataType {
            private Mma8452QCartesianFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            internal Mma8452QCartesianFloatData(byte register, byte copies) : 
                base(ACCELEROMETER, register, new DataAttributes(new byte[] { 2, 2, 2 }, copies, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new Mma8452QCartesianFloatData(input, module, register, id, attributes);
            }

            public override IData createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AccelerationData(bridge, this, timestamp, data);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return 1000f;
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new FloatDataType(this, ACCELEROMETER, DATA_VALUE, new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new FloatDataType(this, ACCELEROMETER, DATA_VALUE, new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new FloatDataType(this, ACCELEROMETER, DATA_VALUE, new DataAttributes(new byte[] { 2 }, 1, 4, true)),
                };
            }
        }

        [DataMember] private readonly byte[] dataSettings = new byte[] { 0x00, 0x00, 0x18, 0x00, 0x00 };
        [DataMember] private Mma8452QCartesianFloatData accDataType, packedAccDataType;

        private IAsyncDataProducer acceleration = null, packedAcceleration = null;
        private TimedTask<byte[]> readConfigTask;

        public AccelerometerMma8452q(IModuleBoardBridge bridge) : base(bridge) {
            accDataType = new Mma8452QCartesianFloatData(DATA_VALUE, 1);
            packedAccDataType = new Mma8452QCartesianFloatData(PACKED_ACC_DATA, 3);
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

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(accDataType);
            collection.Add(packedAccDataType);
        }

        protected override void init() {
            readConfigTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)ACCELEROMETER, Util.setRead(DATA_CONFIG)), 
                response => readConfigTask.SetResult(response));
        }

        public void Configure(OutputDataRate odr, DataRange range) {
            dataSettings[2] |= (byte) ((byte) odr << 3);
            dataSettings[0] |= (byte) range;

            bridge.sendCommand(ACCELEROMETER, DATA_CONFIG, dataSettings);
        }

        public void Configure(float odr = 100f, float range = 2f) {
            Configure((OutputDataRate) Util.closestIndex(FREQUENCIES, odr), (DataRange) Util.closestIndex(RANGES, range));
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
