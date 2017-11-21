using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.ProximityTsl2671;
using static MbientLab.MetaWear.Impl.Module;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ProximityTsl2671 : ModuleImplBase, IProximityTsl2671 {
        private const byte ADC = 1, MODE = 2;

        [DataMember] private DataTypeBase adcData;

        private IForcedDataProducer adcProducer;

        public IForcedDataProducer Adc {
            get {
                if (adcProducer == null) {
                    adcProducer = new ForcedDataProducer(adcData, bridge);
                }
                return adcProducer;
            }
        }

        public ProximityTsl2671(IModuleBoardBridge bridge) : base(bridge) {
            adcData = new IntegralDataType(PROXIMITY, Util.setRead(ADC), new DataAttributes(new byte[] { 2 }, 1, 0, false));
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(adcData);
        }

        public void Configure(ReceiverDiode diode = ReceiverDiode.Channel1, TransmitterDriveCurrent current = TransmitterDriveCurrent._25mA, float integrationTime = 2.72F, byte nPulses = 1) {
            byte pTime = Math.Min(Math.Max((byte)(256f - integrationTime / 2.72f), (byte) 0), (byte) 255);
            byte[] config = new byte[] { pTime, nPulses, (byte)((((int) diode + 1) << 4) | ((int) current << 6)) };
            bridge.sendCommand(PROXIMITY, MODE, config);
        }
    }
}
