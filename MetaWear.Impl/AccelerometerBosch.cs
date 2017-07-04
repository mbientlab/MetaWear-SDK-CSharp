using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(BoschCartesianFloatData))]
    [DataContract]
    abstract class AccelerometerBosch : ModuleImplBase, IAccelerometerBosch {
        private const byte PACKED_ACC_REVISION = 0x1, FLAT_REVISION = 0x2;
        protected const byte POWER_MODE = 1,
            DATA_INTERRUPT_ENABLE = 2, DATA_CONFIG = 3, DATA_INTERRUPT = 4, DATA_INTERRUPT_CONFIG = 5,
            PACKED_ACC_DATA = 0x1c;

        protected static readonly byte[] RANGE_BIT_MASKS = new byte[] { 0x3, 0x5, 0x8, 0xc };
        internal static readonly float[] RANGES = new float[] { 2f, 4f, 8f, 16f };

        [KnownType(typeof(BoschCartesianFloatData))]
        [KnownType(typeof(FloatDataType))]
        [DataContract(IsReference = true)]
        protected class BoschCartesianFloatData : FloatVectorDataType {
            private BoschCartesianFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base (input, module, register, id, attributes) { }

            internal BoschCartesianFloatData(byte register, byte copies) :
                base(ACCELEROMETER, register, new DataAttributes(new byte[] { 2, 2, 2 }, copies, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschCartesianFloatData(input, module, register, id, attributes);
            }

            public override IData createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AccelerationData(bridge, this, timestamp, data);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return (bridge.GetModule<IAccelerometerBosch>() as AccelerometerBosch).GetDataScale();
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new FloatDataType(this, ACCELEROMETER, DATA_INTERRUPT, new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new FloatDataType(this, ACCELEROMETER, DATA_INTERRUPT, new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new FloatDataType(this, ACCELEROMETER, DATA_INTERRUPT, new DataAttributes(new byte[] { 2 }, 1, 4, true)),
                };
            }
        }

        [DataMember] protected BoschCartesianFloatData accDataType, packedAccDataType;

        protected IAsyncDataProducer acceleration = null, packedAcceleration = null;

        public IAsyncDataProducer Acceleration {
            get {
                if (acceleration == null) {
                    acceleration = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, accDataType, bridge);
                }
                return acceleration;
            }
        }

        public IAsyncDataProducer PackedAcceleration {
            get {
                if (bridge.lookupModuleInfo(ACCELEROMETER).revision >= PACKED_ACC_REVISION) {
                    if (packedAcceleration == null) {
                        packedAcceleration = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, packedAccDataType, bridge);
                    }
                    return packedAcceleration;
                }
                return null;
            }
        }

        public AccelerometerBosch(IModuleBoardBridge bridge) : base(bridge) {
            accDataType = new BoschCartesianFloatData(DATA_INTERRUPT, 1);
            packedAccDataType = new BoschCartesianFloatData(PACKED_ACC_DATA, 3);
        }

        
        public abstract void Configure(float odr = 100, float range = 2f);

        public void Start() {
            bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, POWER_MODE, 0x1 });
        }

        public void Stop() {
            bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, POWER_MODE, 0x0 });
        }

        protected abstract float GetDataScale();
    }
}
