using MbientLab.MetaWear.Sensor.BarometerBosch;
using MbientLab.MetaWear.Sensor.BarometerBmp280;

namespace MbientLab.MetaWear.Sensor {
    namespace BarometerBmp280 {
        /// <summary>
        /// Supported stand by times on the BME280 barometer
        /// </summary>
        public enum StandbyTime {
            _0_5ms,
            _62_5ms,
            _125ms,
            _250ms,
            _500ms,
            _1000ms,
            _2000ms,
            _4000ms,
        }
    }
    /// <summary>
    /// Extension of the <seealso cref="IBarometerBosch"/> interface providing finer control over the barometer on
    /// the BMP280 pressure sensor
    /// </summary>
    public interface IBarometerBmp280 : IBarometerBosch {
        /// <summary>
        /// Configure the snsor with settings specific to the BMP280 barometer
        /// </summary>
        /// <param name="os">Oversampling mode, defaults to standard</param>
        /// <param name="coeff">IIR coefficient, defaults to 0 (off)</param>
        /// <param name="standbyTime">Standby time in milliseconds (ms), defaults to 0.5ms</param>
        void Configure(Oversampling os = Oversampling.Standard, IirFilerCoeff coeff = IirFilerCoeff._0, StandbyTime standbyTime = StandbyTime._0_5ms);
    }
}
