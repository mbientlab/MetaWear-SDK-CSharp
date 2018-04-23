using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.GyroBmi160;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(Bmi160GyroCartesianFloatData))]
    [DataContract]
    class GyroBmi160 : ModuleImplBase, IGyroBmi160 {
        private const byte PACKED_GYRO_REVISION = 1;
        private const byte POWER_MODE = 1, DATA_INTERRUPT_ENABLE = 2, CONFIG = 3, DATA = 5, PACKED_DATA = 0x7;
        private static readonly float[] FSR_SCALE = { 16.4f, 32.8f, 65.6f, 131.2f, 262.4f };

        internal static string createIdentifier(DataTypeBase dataType) {
            switch (dataType.eventConfig[1]) {
                case DATA:
                    return dataType.attributes.length() > 2 ? "angular-velocity" : string.Format("angular-velocity[{0}]", (dataType.attributes.offset >> 1));
                case PACKED_DATA:
                    // packed data is handled exactly the same as unpacked so give them the same identifier
                    return "angular-velocity";
                default:
                    return null;
            }
        }

        [KnownType(typeof(Bmi160GyroCartesianFloatData))]
        [KnownType(typeof(FloatDataType))]
        [DataContract]
        protected class Bmi160GyroCartesianFloatData : FloatVectorDataType {
            private Bmi160GyroCartesianFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            internal Bmi160GyroCartesianFloatData(byte register, byte copies) :
                base(GYRO, register, new DataAttributes(new byte[] { 2, 2, 2 }, copies, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new Bmi160GyroCartesianFloatData(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AngularVelocityData(bridge, this, timestamp, data);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return (bridge.GetModule<IGyroBmi160>() as GyroBmi160).Scale;
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 4, true)),
                };
            }
        }

        [DataMember] private readonly byte[] gyrDataConfig = new byte[] { 0x20 | (byte)OutputDataRate._100Hz, (byte)DataRange._2000dps };
        [DataMember] private readonly Bmi160GyroCartesianFloatData spinDataType, packedSpinDataType;
        private TimedTask<byte[]> readConfigTask;

        private IAsyncDataProducer angularVelocity = null, packedAngularVelocity = null;

        public IAsyncDataProducer AngularVelocity {
            get {
                if (angularVelocity == null) {
                    angularVelocity = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, spinDataType, bridge);
                }
                return angularVelocity;
            }
        }

        public IAsyncDataProducer PackedAngularVelocity {
            get {
                if (bridge.lookupModuleInfo(GYRO).revision >= PACKED_GYRO_REVISION) {
                    if (packedAngularVelocity == null) {
                        packedAngularVelocity = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, packedSpinDataType, bridge);
                    }
                    return packedAngularVelocity;
                }
                return null;
            }
        }

        private float Scale {
            get {
                return FSR_SCALE[gyrDataConfig[1] & 0x7];
            }
        }

        public GyroBmi160(IModuleBoardBridge bridge) : base(bridge) {
            spinDataType = new Bmi160GyroCartesianFloatData(DATA, 1);
            packedSpinDataType = new Bmi160GyroCartesianFloatData(PACKED_DATA, 3);
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(spinDataType);
            collection.Add(packedSpinDataType);
        }

        protected override void init() {
            readConfigTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)GYRO, Util.setRead(CONFIG)), response => readConfigTask.SetResult(response));
        }

        public void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._125dps, FilterMode filter = FilterMode.Normal) {
            gyrDataConfig[1] &= 0xf8;
            gyrDataConfig[1] |= (byte) range;
            gyrDataConfig[0] = (byte) ((int) odr + 6 | (int) filter << 4);

            bridge.sendCommand(GYRO, CONFIG, gyrDataConfig);
        }

        public void Start() {
            bridge.sendCommand(new byte[] { (byte) GYRO, POWER_MODE, 1 });
        }

        public void Stop() {
            bridge.sendCommand(new byte[] { (byte)GYRO, POWER_MODE, 0 });
        }

        public async Task PullConfigAsync() {
            byte[] response = await readConfigTask.Execute("Did not receive gyro config within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)GYRO, Util.setRead(CONFIG) }));
            Array.Copy(response, 2, gyrDataConfig, 0, gyrDataConfig.Length);
        }
    }
}
