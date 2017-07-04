using MbientLab.MetaWear.Sensor.BarometerBosch;

namespace MbientLab.MetaWear.Sensor {
    namespace BarometerBosch {
        /// <summary>
        /// Supported oversampling modes on a Bosch barometer
        /// </summary>
        public enum Oversampling {
            Skip,
            UltraLowPower,
            LowPower,
            Standard,
            High,
            UltraHigh
        }

        /// <summary>
        /// Available IIR (infinite impulse response) filter coefficients
        /// </summary>
        public enum IirFilerCoeff {
            _0,
            _2,
            _4,
            _8,
            _16
        }
    }
    /// <summary>
    /// Absolute barometric pressure sensor by Bosch.  This interface provides general access to a Bosch
    /// barometer.If you know specifically which barometer is on your board, use the appropriate subclass
    /// instead.
    /// <seealso cref="IBarometerBme280"/>
    /// <seealso cref="IBarometerBmp280"/>
    /// </summary>
    public interface IBarometerBosch : IModule {
        /// <summary>
        /// Data producer representing pressure data
        /// </summary>
        IAsyncDataProducer Pressure { get; }
        /// <summary>
        /// Data producer representing altitude data
        /// </summary>
        IAsyncDataProducer Altitude { get; }
        /// <summary>
        /// General function to configure the barometer.  The closest valid standby time will be selected 
        /// based on the underlying barometer which may not match the input value
        /// </summary>
        /// <param name="os">Oversampling mode, defaults to standard</param>
        /// <param name="coeff">IIR coefficient, defaults to 0 (off)</param>
        /// <param name="standbyTime">Standby time in milliseconds (ms), defaults to 0.5ms</param>
        void Configure(Oversampling os = Oversampling.Standard, IirFilerCoeff coeff = IirFilerCoeff._0, float standbyTime = 0.5f);

        /// <summary>
        /// Start data sampling
        /// </summary>
        void Start();
        /// <summary>
        /// Stop data sampling
        /// </summary>
        void Stop();
    }
}
