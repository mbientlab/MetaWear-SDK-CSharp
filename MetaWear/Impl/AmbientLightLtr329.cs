using static MbientLab.MetaWear.Impl.Module;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.AmbientLightLtr329;

using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class AmbientLightLtr329 : ModuleImplBase, IAmbientLightLtr329 {
        private const byte ENABLE = 1, CONFIG = 2, OUTPUT = 3;

        [DataMember] private DataTypeBase illuminanceData;

        private AsyncDataProducer illuminanceProducer;

        public IAsyncDataProducer Illuminance {
            get {
                if (illuminanceProducer == null) {
                    illuminanceProducer = new AsyncDataProducer(ENABLE, illuminanceData, bridge);
                }
                return illuminanceProducer;
            }
        }

        public AmbientLightLtr329(IModuleBoardBridge bridge) : base(bridge) {
            illuminanceData = new MilliUnitsFloatDataType(AMBIENT_LIGHT, OUTPUT, new DataAttributes(new byte[] { 4 }, 1, 0, false));
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(illuminanceData);
        }

        public void Configure(Gain gain = Gain._1x, IntegrationTime time = IntegrationTime._100ms, MeasurementRate rate = MeasurementRate._500ms) {
            int gainMask = gain == Gain._48x || gain == Gain._96x ? (int)gain + 2 : (int)gain;
            bridge.sendCommand(new byte[] { (byte) AMBIENT_LIGHT, CONFIG, (byte) (gainMask << 2), (byte) ((int) time << 3 | (int) rate) });
        }
    }
}
