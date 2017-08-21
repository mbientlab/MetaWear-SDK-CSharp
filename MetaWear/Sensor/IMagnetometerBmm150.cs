using MbientLab.MetaWear.Sensor.MagnetometerBmm150;

namespace MbientLab.MetaWear.Sensor {
    namespace MagnetometerBmm150 {
        /// <summary>
        /// Recommended configurations for the magnetometer
        /// </summary>
        public enum Preset {
            /// <summary>
            /// 10Hz, 170uA
            /// </summary>
            LowPower,
            /// <summary>
            /// 10Hz, 0.5mA
            /// </summary>
            Regular,
            /// <summary>
            /// 10Hz, 0.8mA
            /// </summary>
            EnhancedRegular,
            /// <summary>
            /// 20Hz, 4.9mA
            /// </summary>
            HighAccuracy
        }
        /// <summary>
        /// Supported output data rates for the BMM150 sensor
        /// </summary>
        public enum OutputDataRate {
            _10Hz,
            _2Hz,
            _6Hz,
            _8Hz,
            _15Hz,
            _20Hz,
            _25Hz,
            _30Hz
        }
    }

    /// <summary>
    /// Bosch sensor measuring magnetic field strength
    /// </summary>
    public interface IMagnetometerBmm150 : IModule {
        /// <summary>
        /// Data producer representing the magnetic field strength
        /// </summary>
        IAsyncDataProducer MagneticField { get; }
        /// <summary>
        /// Variant data producer that packs 3 B field samples in to 1 ble packet.  
        /// Only streaming is supported by this data producer
        /// </summary>
        IAsyncDataProducer PackedMagneticField { get; }

        /// <summary>
        /// Configure the magnetometer
        /// </summary>
        /// <param name="xyReps">Number of repetitions on the XY axis, between [1, 511], defauts to 9 reps</param>
        /// <param name="zReps">Number of repetitions on the Z axis, between [1, 256], defaut to 15 reps</param>
        /// <param name="odr">Output data rate, defaults to 10Hz</param>
        void Configure(ushort xyReps = 9, ushort zReps = 15, OutputDataRate odr = OutputDataRate._10Hz);
        /// <summary>
        /// Apply a preset configuration
        /// </summary>
        /// <param name="preset">Preset configuration to use</param>
        void Configure(Preset preset);

        /// <summary>
        /// Switch the magnetometer into normal mode
        /// </summary>
        void Start();
        /// <summary>
        /// Switch the magnetometer into sleep mode
        /// </summary>
        void Stop();
        /// <summary>
        /// Switch the magnetometer into suspend mode.  When placed in suspend mode, sensor settings are reset 
        /// and will need to be reconfigured.
        /// </summary>
        void Suspend();
    }
}
