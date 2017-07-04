using MbientLab.MetaWear.Sensor.AccelerometerBma255;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Sensor {
    namespace AccelerometerBma255 {
        /// <summary>
        /// Operating frequencies of the BMA255accelerometer
        /// </summary>
        public enum OutputDataRate {
            _15_62Hz,
            _31_26Hz,
            _62_5Hz,
            _125Hz,
            _250Hz,
            _500Hz,
            _1000Hz,
            _2000Hz
        }
    }

    /// <summary>
    /// Extension of the <see cref="IAccelerometer"/> interface providing finer control of the BMA255 accelerometer
    /// </summary>
    public interface IAccelerometerBma255 : IAccelerometerBosch {
        /// <summary>
        /// Configure the snsor with settings specific to the BMA255 accelerometer
        /// </summary>
        /// <param name="odr">Output data rate</param>
        /// <param name="range">Data range</param>
        void Configure(OutputDataRate odr = OutputDataRate._125Hz, DataRange range = DataRange._2g);
    }
}
