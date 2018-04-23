using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using MbientLab.MetaWear.Sensor.BarometerBosch;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(BarometerBoschFloatDataType))]
    [DataContract]
    abstract class BarometerBosch : ModuleImplBase, IBarometerBosch {
        internal static string createIdentifier(DataTypeBase dataType) {
            switch (dataType.eventConfig[1]) {
                case PRESSURE:
                    return "pressure";
                case ALTITUDE:
                    return "altitude";
                default:
                    return null;
            }
        }
        internal const byte PRESSURE = 1, ALTITUDE = 2, CONFIG = 3, CYCLIC = 4;

        [DataContract]
        private class BarometerBoschFloatDataType : FloatDataType {
            internal BarometerBoschFloatDataType(byte register, bool signed) :
                base(BAROMETER, register, new DataAttributes(new byte[] { 4 }, 1, 0, signed)) { }

            internal BarometerBoschFloatDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes)
                : base(input, module, register, id, attributes) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BarometerBoschFloatDataType(input, module, register, id, attributes);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return 256f;
            }
        }

        private class BoschPressureDataProducer : AsyncDataProducer {
            internal BoschPressureDataProducer(DataTypeBase datatype, IModuleBoardBridge bridge) : base(datatype, bridge) { }

            public override void Start() {
            }

            public override void Stop() {
            }
        }

        private class BoschAltitudeDataProducer : AsyncDataProducer {
            internal BoschAltitudeDataProducer(DataTypeBase datatype, IModuleBoardBridge bridge) : base(datatype, bridge) { }

            public override void Start() {
                (bridge.GetModule<IBarometerBosch>() as BarometerBosch).enableAltitude = 1;
            }

            public override void Stop() {
                (bridge.GetModule<IBarometerBosch>() as BarometerBosch).enableAltitude = 0;
            }
        }

        [DataMember] private BarometerBoschFloatDataType pressureDataType, altitudeDataType;
        [DataMember] private byte enableAltitude = 0;

        private AsyncDataProducer pressureDataProducer, altitudeDataProducer;

        public BarometerBosch(IModuleBoardBridge bridge) : base(bridge) {
            pressureDataType = new BarometerBoschFloatDataType(PRESSURE, false);
            altitudeDataType = new BarometerBoschFloatDataType(ALTITUDE, true);
        }

        public IAsyncDataProducer Pressure {
            get {
                if (pressureDataProducer == null) {
                    pressureDataProducer = new BoschPressureDataProducer(pressureDataType, bridge);
                }
                return pressureDataProducer;
            }
        }

        public IAsyncDataProducer Altitude {
            get {
                if (altitudeDataProducer == null) {
                    altitudeDataProducer = new BoschAltitudeDataProducer(altitudeDataType, bridge);
                }
                return altitudeDataProducer;
            }
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(pressureDataType);
            collection.Add(altitudeDataType);
        }

        public void Start() {
            bridge.sendCommand(new byte[] { (byte) BAROMETER, CYCLIC, 1, enableAltitude });
        }

        public void Stop() {
            bridge.sendCommand(new byte[] { (byte) BAROMETER, CYCLIC, 0, 0 });
        }

        public abstract void Configure(Oversampling os = Oversampling.Standard, IirFilerCoeff coeff = IirFilerCoeff._0, float standbyTime = 0.5F);
    }
}
