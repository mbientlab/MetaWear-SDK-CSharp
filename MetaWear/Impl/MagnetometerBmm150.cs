using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.MagnetometerBmm150;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(Bmm150CartesianFloatData))]
    [DataContract]
    class MagnetometerBmm150 : ModuleImplBase, IMagnetometerBmm150 {
        private const byte PACKED_MAG_REVISION = 1, SUSPEND_REVISION = 2;
        private const byte POWER_MODE = 1,
                DATA_INTERRUPT_ENABLE = 2, DATA_RATE = 3, DATA_REPETITIONS = 4, MAG_DATA = 5,
                PACKED_MAG_DATA = 0x09;

        internal static string createIdentifier(DataTypeBase dataType) {
            switch (dataType.eventConfig[1]) {
                case MAG_DATA:
                    return dataType.attributes.length() > 2 ? "magnetic-field" : string.Format("magnetic-field[{0}]", (dataType.attributes.offset >> 1));
                case PACKED_MAG_DATA:
                    // packed data is handled exactly the same as unpacked so give them the same identifier
                    return "magnetic-field";
                default:
                    return null;
            }
        }

        [KnownType(typeof(Bmm150CartesianFloatData))]
        [KnownType(typeof(FloatDataType))]
        [DataContract]
        protected class Bmm150CartesianFloatData : FloatVectorDataType {
            private Bmm150CartesianFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            internal Bmm150CartesianFloatData(byte register, byte copies) :
                base(MAGNETOMETER, register, new DataAttributes(new byte[] { 2, 2, 2 }, copies, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new Bmm150CartesianFloatData(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new MagneticFieldData(bridge, this, timestamp, data);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return 16000000f;
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 4, true))
                };
            }
        }

        [DataMember] private readonly Bmm150CartesianFloatData bFieldDataType, packedBFieldDataType;

        private IAsyncDataProducer bField = null, packedBField = null;

        public IAsyncDataProducer MagneticField {
            get {
                if (bField == null) {
                    bField = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, bFieldDataType, bridge);
                }
                return bField;
            }
        }

        public IAsyncDataProducer PackedMagneticField {
            get {
                if (bridge.lookupModuleInfo(MAGNETOMETER).revision >= PACKED_MAG_REVISION) {
                    if (packedBField == null) {
                        packedBField = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, packedBFieldDataType, bridge);
                    }
                    return packedBField;
                }
                return null;
            }
        }

        public MagnetometerBmm150(IModuleBoardBridge bridge) : base(bridge) {
            bFieldDataType = new Bmm150CartesianFloatData(MAG_DATA, 1);
            packedBFieldDataType = new Bmm150CartesianFloatData(PACKED_MAG_DATA, 3);
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(bFieldDataType);
            collection.Add(packedBFieldDataType);
        }

        public void Configure(ushort xyReps = 9, ushort zReps = 15, OutputDataRate odr = OutputDataRate._10Hz) {
            if (bridge.lookupModuleInfo(MAGNETOMETER).revision >= SUSPEND_REVISION) {
                Stop();
            }
            bridge.sendCommand(new byte[] { (byte) MAGNETOMETER, DATA_REPETITIONS, (byte)((xyReps - 1) / 2), (byte)(zReps - 1) });
            bridge.sendCommand(new byte[] { (byte) MAGNETOMETER, DATA_RATE, (byte)odr });
        }

        public void Configure(Preset preset) {
            switch (preset) {
                case Preset.LowPower:
                    Configure(3, 3);
                    break;
                case Preset.Regular:
                    Configure();
                    break;
                case Preset.EnhancedRegular:
                    Configure(15, 27);
                    break;
                case Preset.HighAccuracy:
                    Configure(47, 83, OutputDataRate._20Hz);
                    break;
            }
        }

        public void Start() {
            bridge.sendCommand(new byte[] { (byte) MAGNETOMETER, POWER_MODE, 1 });
        }

        public void Stop() {
            bridge.sendCommand(new byte[] { (byte) MAGNETOMETER, POWER_MODE, 0 });
        }

        public void Suspend() {
            if (bridge.lookupModuleInfo(MAGNETOMETER).revision >= SUSPEND_REVISION) {
                bridge.sendCommand(new byte[] { (byte)MAGNETOMETER, POWER_MODE, 2 });
            }
        }
    }
}
