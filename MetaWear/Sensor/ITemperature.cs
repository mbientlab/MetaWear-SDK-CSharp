using MbientLab.MetaWear.Sensor.Temperature;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Sensor {
    namespace Temperature {
        /// <summary>
        /// Available types of temperature sensors.  Different boards will have a different combination
        /// of sensor types
        /// </summary>
        public enum SensorType {
            /// <summary>
            /// Data from nRF SOC
            /// </summary>
            NrfSoc,
            /// <summary>
            /// External thermistor connected to the GPIO pins
            /// </summary>
            ExtThermistor,
            /// <summary>
            /// Data from a Bosch environmental sensor (BMP280 or BME280)
            /// </summary>
            BoschEnv,
            /// <summary>
            /// Preset thermistor on the module
            /// </summary>
            PresetThermistor
        }
        /// <summary>
        /// Data producer representing temperature data
        /// </summary>
        public interface ISensor : IForcedDataProducer {
            /// <summary>
            /// Type of temperature sensor this objec represents
            /// </summary>
            SensorType Type { get; }
        }
        /// <summary>
        /// Data producer representing an external thermistor
        /// </summary>
        public interface IExternalThermistor : ISensor {
            /// <summary>
            /// Configures the settings for the thermistor
            /// </summary>
            /// <param name="dataPin">GPIO pin that reads the data</param>
            /// <param name="pulldownPin">GPIO pin the pulldown resistor is connected to</param>
            /// <param name="activeHigh">True if the pulldown pin is active high</param>
            void Configure(byte dataPin, byte pulldownPin, bool activeHigh);
        }
    }
    /// <summary>
    /// Accesses the temperature sensors
    /// </summary>
    public interface ITemperature : IModule {
        /// <summary>
        /// List of available temperature sensors on the board
        /// </summary>
        List<ISensor> Sensors { get; }
        /// <summary>
        /// Find all temperature sensors whose <see cref="ISensor.Type"/> property matches the <code>type</code> parameter
        /// </summary>
        /// <param name="type">Sensor type to look for</param>
        /// <returns>List of sensors matching the sensor type</returns>
        List<ISensor> FindSensors(SensorType type);
    }
}
