using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using MbientLab.MetaWear.Sensor.BarometerBosch;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(HumidityFLoatData))]
    [DataContract]
    class HumidityBme280 : ModuleImplBase, IHumidityBme280 {
        private const byte VALUE = 1, MODE = 2;

        [DataContract]
        private class HumidityFLoatData : FloatDataType {
            internal HumidityFLoatData() :
                base(HUMIDITY, Util.setRead(VALUE), new DataAttributes(new byte[] { 4 }, 1, 0, false)) { }

            internal HumidityFLoatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            public override float scale(IModuleBoardBridge bridge) {
                return 1024f;
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new HumidityFLoatData(input, module, register, id, attributes);
            }
        }

        [DataMember] private HumidityFLoatData humidityData;
        private IForcedDataProducer humidityDataProducer;

        public IForcedDataProducer Percentage {
            get {
                if (humidityDataProducer == null) {
                    humidityDataProducer = new ForcedDataProducer(humidityData, bridge);
                }
                return humidityDataProducer;
            }
        }

        public HumidityBme280(IModuleBoardBridge bridge) : base(bridge) {
            humidityData = new HumidityFLoatData();
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(humidityData);
        }

        public void Configure(Oversampling os = Oversampling.Standard) {
            bridge.sendCommand(new byte[] { (byte) HUMIDITY, MODE, (byte) os });
        }
    }
}
