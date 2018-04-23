using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.BarometerBmp280;
using MbientLab.MetaWear.Sensor.BarometerBosch;
using System.Runtime.Serialization;
using static MbientLab.MetaWear.Impl.Module;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class BarometerBmp280 : BarometerBosch, IBarometerBmp280 {
        internal const byte IMPLEMENTATION = 0;
        private static readonly float[] STANDBY_TIMES = new float[] {
            0.5f, 62.5f, 125f, 250f, 500f, 1000f, 2000f, 4000f
        };

        public BarometerBmp280(IModuleBoardBridge bridge) : base(bridge) {
        }

        public void Configure(Oversampling os = Oversampling.Standard, IirFilerCoeff coeff = IirFilerCoeff._0, StandbyTime standbyTime = StandbyTime._0_5ms) {
            var tempOversampling = (byte)((os == Oversampling.UltraHigh) ? 2 : 1);
            bridge.sendCommand(new byte[] {(byte) BAROMETER, CONFIG,
                        (byte) (((byte) os << 2) | (tempOversampling << 5)),
                        (byte) (((byte) coeff << 2) | ((byte) standbyTime << 5))});
        }

        public override void Configure(Oversampling os = Oversampling.Standard, IirFilerCoeff coeff = IirFilerCoeff._0, float standbyTime = 0.5f) {
            int index = Util.ClosestIndex_float(STANDBY_TIMES, standbyTime);
            Configure(os, coeff, (StandbyTime)index);
        }
    }
}
