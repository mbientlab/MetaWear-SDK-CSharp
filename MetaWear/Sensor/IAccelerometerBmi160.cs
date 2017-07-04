using MbientLab.MetaWear.Sensor.AccelerometerBmi160;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Sensor {
    namespace AccelerometerBmi160 {
        /// <summary>
        /// Operating frequencies of the BMI160 accelerometer
        /// </summary>
        public enum OutputDataRate {
            _0_78125Hz,
            _1_5625Hz,
            _3_125Hz,
            _6_25Hz,
            _12_5Hz,
            _25Hz,
            _50Hz,
            _100Hz,
            _200Hz,
            _400Hz,
            _800Hz,
            _1600Hz
        }
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometerBosch"/> interface providing finer control of the BMI160 accelerometer features
    /// </summary>
    public interface IAccelerometerBmi160 : IAccelerometerBosch {
        /// <summary>
        /// Configure the snsor with settings specific to the BMA255 accelerometer
        /// </summary>
        /// <param name="odr">Output data rate</param>
        /// <param name="range">Data range</param>
        void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g);
    }
}
