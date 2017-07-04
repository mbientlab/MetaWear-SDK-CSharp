using MbientLab.MetaWear.Sensor.AmbientLightLtr329;

namespace MbientLab.MetaWear.Sensor {
    namespace AmbientLightLtr329 {
        /// <summary>
        /// Available gain multipliers
        /// </summary>
        public enum Gain {
            /// <summary>
            /// Illuminance range between [1, 64k] lux (default)
            /// </summary>
            _1x,
            /// <summary>
            /// Illuminance range between [0.5, 32k] lux
            /// </summary>
            _2x,
            /// <summary>
            /// Illuminance range between [0.25, 16k]
            /// </summary>
            _4x,
            /// <summary>
            /// Illuminance range between [0.125, 8k]
            /// </summary>
            _8x,
            /// <summary>
            /// Illuminance range between [0.02, 1.3k]
            /// </summary>
            _48x,
            /// <summary>
            /// Illuminance range between [0.01, 600]
            /// </summary>
            _96x
        }
        /// <summary>
        /// Available integration times
        /// </summary>
        public enum IntegrationTime {
            /// <summary>
            /// Default setting
            /// </summary>
            _100ms,
            _50ms,
            _200ms,
            _400ms,
            _150ms,
            _250ms,
            _300ms,
            _350ms
        }
        /// <summary>
        /// Available measurement rates
        /// </summary>
        public enum MeasurementRate {
            _50ms,
            _100ms,
            _200ms,
            /// <summary>
            /// Default setting
            /// </summary>
            _500ms,
            _1000ms,
            _2000ms
        }
    }
    /// <summary>
    /// Lite-On sensor converting light intensity to a digital signal
    /// </summary>
    public interface IAmbientLightLtr329 : IModule {
        /// <summary>
        /// Data producer for illuminance data
        /// </summary>
        IAsyncDataProducer Illuminance { get; }
        /// <summary>
        /// Configures the ambient light sensor
        /// </summary>
        /// <param name="gain">Controls the range and resolution of illuminance values, defaults to 1x</param>
        /// <param name="time">Measurement time for each cycle, defaults to 100mx</param>
        /// <param name="rate">How frequently to update the illuminance data, defaults to 500ms</param>
        void Configure(Gain gain = Gain._1x, IntegrationTime time = IntegrationTime._100ms, MeasurementRate rate = MeasurementRate._500ms);
    }
}
