using MbientLab.MetaWear.Sensor.GyroBmi160;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Sensor {
    namespace GyroBmi160 {
        /// <summary>
        /// Gyro digital filter modes on the BMI160
        /// </summary>
        public enum FilterMode {
            Osr4,
            Osr2,
            Normal
        }
        /// <summary>
        /// Operating frequencies of the gyro
        /// </summary>
        public enum OutputDataRate {
            _25Hz,
            _50Hz,
            _100Hz,
            _200Hz,
            _400Hz,
            _800Hz,
            _1600Hz,
            _3200Hz
        }
        /// <summary>
        /// Available angular velocity measurement ranges
        /// </summary>
        public enum DataRange {
            _2000dps,
            _1000dps,
            _500dps,
            _250dps,
            _125dps
        }
    }
    /// <summary>
    /// Sensor on the BMI160 IMU measuring angular velocity
    /// </summary>
    public interface IGyroBmi160 : IModule {
        /// <summary>
        /// Data producer representing the sensor's angular velocity data
        /// </summary>
        IAsyncDataProducer AngularVelocity { get; }
        /// <summary>
        /// Variant data producer that packs 3 angular velocity samples in to 1 ble packet.  
        /// Only streaming is supported by this data producer
        /// </summary>
        IAsyncDataProducer PackedAngularVelocity { get; }

        /// <summary>
        /// Configure the snsor with settings specific to the BMI160 gyro
        /// </summary>
        /// <param name="odr">Output data rate</param>
        /// <param name="range">Data range</param>
        /// <param name="filter">Accelerometer digital filter mode</param>
        void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._125dps, FilterMode filter = FilterMode.Normal);
        /// <summary>
        /// Switch the gyro into active mode
        /// </summary>
        void Start();
        /// <summary>
        /// Switch the gyro into standby mode
        /// </summary>
        void Stop();

        /// <summary>
        /// Pulls the current gyro output data rate and data range from the sensor
        /// </summary>
        /// <returns>Task that is completed when the settings are received</returns>
        Task PullConfigAsync();
    }
}
