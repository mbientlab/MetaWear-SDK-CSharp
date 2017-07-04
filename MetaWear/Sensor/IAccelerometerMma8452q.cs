using MbientLab.MetaWear.Sensor.AccelerometerMma8452q;

namespace MbientLab.MetaWear.Sensor {
    namespace AccelerometerMma8452q {
        /// <summary>
        /// Available data ranges for the MMA8452Q
        /// </summary>
        public enum DataRange {
            _2g,
            _4g,
            _8g
        }
        /// <summary>
        /// Available operating frequencies for the MMA8452Q
        /// </summary>
        public enum OutputDataRate {
            _800Hz,
            _400Hz,
            _200Hz,
            _100Hz,
            _50Hz,
            _12_5Hz,
            _6_25Hz,
            _1_56Hz
        }
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometer"/> interface providing finer control of the MMA8452Q accelerometer
    /// </summary>
    public interface IAccelerometerMma8452q : IAccelerometer {
        /// <summary>
        /// Configure the snsor with settings specific to the BMI160 accelerometer
        /// </summary>
        /// <param name="odr">Output data rate</param>
        /// <param name="range">Data range</param>
        void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g);
    }
}
