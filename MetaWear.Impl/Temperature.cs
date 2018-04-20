using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System.Collections.Generic;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor.Temperature;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(ExternalThermistor))]
    [KnownType(typeof(TemperatureSensor))]
    [DataContract]
    class Temperature : ModuleImplBase, ITemperature {
        private const byte VALUE = 1, MODE = 2;

        [DataContract]
        private class TemperatureFloatData : FloatDataType {
            internal TemperatureFloatData(byte id) : 
                base(TEMPERATURE, Util.setRead(VALUE), id, new DataAttributes(new byte[] { 2 }, 1, 0, true)) { }

            internal TemperatureFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            public override float scale(IModuleBoardBridge bridge) {
                return 8f;
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new TemperatureFloatData(input, module, register, id, attributes);
            }
        }

        [KnownType(typeof(TemperatureFloatData))]
        [DataContract]
        private class TemperatureSensor : ForcedDataProducer, ISensor {
            [DataMember] private readonly SensorType type;

            public SensorType Type {
                get {
                    return type;
                }
            }

            internal TemperatureSensor(SensorType type, DataTypeBase dataType, IModuleBoardBridge bridge) : base(dataType, bridge) {
                this.type = type;
            }
        }

        [DataContract]
        private class ExternalThermistor : TemperatureSensor, IExternalThermistor {
            internal ExternalThermistor(DataTypeBase dataType, IModuleBoardBridge bridge) : base(SensorType.ExtThermistor, dataType, bridge) {
            }

            public void Configure(byte dataPin, byte pulldownPin, bool activeHigh) {
                bridge.sendCommand(new byte[] { (byte) TEMPERATURE, MODE, dataTypeBase.eventConfig[2], dataPin, pulldownPin, (byte)(activeHigh ? 1 : 0) });
            }
        }

        [DataMember] private readonly List<ISensor> sensors;
        [DataMember] private readonly List<DataTypeBase> tempDataTypes;

        public Temperature(IModuleBoardBridge bridge) : base(bridge) {
            var info = bridge.lookupModuleInfo(TEMPERATURE);

            byte i = 0;
            sensors = new List<ISensor>();
            tempDataTypes = new List<DataTypeBase>();
            foreach (byte it in info.extra) {
                var dataType = new TemperatureFloatData(i);
                tempDataTypes.Add(dataType);

                switch(it) {
                    case (byte)SensorType.NrfSoc:
                        sensors.Add(new TemperatureSensor(SensorType.NrfSoc, dataType, bridge));
                        break;
                    case (byte)SensorType.ExtThermistor:
                        sensors.Add(new ExternalThermistor(dataType, bridge));
                        break;
                    case (byte)SensorType.BoschEnv:
                        sensors.Add(new TemperatureSensor(SensorType.BoschEnv, dataType, bridge));
                        break;
                    case (byte)SensorType.PresetThermistor:
                        sensors.Add(new TemperatureSensor(SensorType.PresetThermistor, dataType, bridge));
                        break;
                }
                i++;
            }
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            foreach(var it in tempDataTypes) {
                collection.Add(it);
            }
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            foreach (TemperatureSensor it in sensors) {
                it.restoreTransientVars(bridge);
            }
        }

        public List<ISensor> Sensors {
            get {
                return sensors;
            }
        }

        public List<ISensor> FindSensors(SensorType type) {
            return sensors.FindAll(s => s.Type == type);
        }
    }
}
