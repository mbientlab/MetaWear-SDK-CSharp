using System;
using static MbientLab.MetaWear.Impl.Module;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.ColorTcs34725;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ColorTcs34725 : ModuleImplBase, IColorTcs34725 {
        private const byte ADC = 1, MODE = 2;

        [KnownType(typeof(IntegralDataType))]
        [KnownType(typeof(AdcDataType))]
        [DataContract]
        private class AdcDataType : DataTypeBase {
            class AdcData : DataBase {
                public AdcData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) :
                    base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(Adc) };

                public override T Value<T>() {
                    var type = typeof(T);

                    if (type == typeof(Adc)) {
                        return (T)Convert.ChangeType(new Adc(
                            BitConverter.ToUInt16(bytes, 0),
                            BitConverter.ToUInt16(bytes, 2),
                            BitConverter.ToUInt16(bytes, 4),
                            BitConverter.ToUInt16(bytes, 6)), type);
                    }
                    return base.Value<T>();
                }
            }

            private AdcDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            internal AdcDataType() :
                base(COLOR_DETECTOR, Util.setRead(ADC), new DataAttributes(new byte[] { 2, 2, 2, 2}, 1, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new AdcDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AdcData(bridge, this, timestamp, data);
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new IntegralDataType((Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new IntegralDataType((Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new IntegralDataType((Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 4, true)),
                    new IntegralDataType((Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 6, true)),
                };
            }

            internal override Tuple<DataTypeBase, DataTypeBase> transform(DataProcessorConfig config, DataProcessor dpModule) {
                switch (config.id) {
                    case DataProcessorConfig.CombinerConfig.ID: {
                        DataAttributes attributes = new DataAttributes(new byte[] { this.attributes.sizes[0] }, 1, 0, false);
                        return Tuple.Create<DataTypeBase, DataTypeBase>(new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes), null);
                    }
                }

                return base.transform(config, dpModule);
            }
        }

        [DataMember] private AdcDataType adcDataType;

        private IForcedDataProducer adcProducer = null;

        public IForcedDataProducer Adc {
            get {
                if (adcProducer == null) {
                    adcProducer = new ForcedDataProducer(adcDataType, bridge);
                }
                return adcProducer;
            }
        }

        public ColorTcs34725(IModuleBoardBridge bridge) : base(bridge) {
            adcDataType = new AdcDataType();
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(adcDataType);
        }

        public void Configure(Gain gain = Gain._1x, float integationTime = 2.4F, bool illuminate = false) {
            bridge.sendCommand(new byte[] { (byte) COLOR_DETECTOR, MODE, (byte)(256f - integationTime / 2.4f), (byte)gain, (byte) (illuminate ? 1 : 0) });
        }
    }
}
