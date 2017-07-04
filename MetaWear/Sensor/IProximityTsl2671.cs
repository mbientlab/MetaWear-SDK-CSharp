using MbientLab.MetaWear.Sensor.ProximityTsl2671;

namespace MbientLab.MetaWear.Sensor {
    namespace ProximityTsl2671 {
        /// <summary>
        /// Photodiodes the sensor should use for proximity detection
        /// </summary>
        public enum ReceiverDiode {
            /// <summary>
            /// Responsive to both visible and infrared light
            /// </summary>
            Channel0,
            /// <summary>
            /// Responsive primarily to infrared light
            /// </summary>
            Channel1,
            /// <summary>
            /// Use both photodiodes
            /// </summary>
            Both
        }
        /// <summary>
        /// Amount of current to drive the sensor
        /// </summary>
        public enum TransmitterDriveCurrent {
            _100mA,
            _50mA,
            _25mA,
            _12_5mA
        }
    }
    /// <summary>
    /// Digital proximity detector for short-distance detection by AMS
    /// </summary>
    public interface IProximityTsl2671 : IModule {
        /// <summary>
        /// Data producer representing measured adc values
        /// </summary>
        IForcedDataProducer Adc { get; }

        /// <summary>
        /// Configure the proximity detector
        /// </summary>
        /// <param name="diode">Photodiode responding to light, defaults to <see cref="ReceiverDiode.Channel1"/></param>
        /// <param name="current">Led drive current, defaults to <see cref="TransmitterDriveCurrent._25mA"/>.  
        /// For boards powered by the CR2032 battery, it is recommended to use 25mA or less</param>
        /// <param name="integrationTime">Period of time the internal ADC converts the analog signal into digital counts, defaults to 2.72ms</param>
        /// <param name="nPulses">Number of pulses, defaults to 1.  Sensitivity grows by the square root of the number of pulses</param>
        void Configure(ReceiverDiode diode = ReceiverDiode.Channel1, TransmitterDriveCurrent current = TransmitterDriveCurrent._25mA, 
            float integrationTime = 2.72f, byte nPulses = 1);
    }
}
