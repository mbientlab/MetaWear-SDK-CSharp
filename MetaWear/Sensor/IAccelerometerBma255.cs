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

        /// <summary>
        /// Enumeration of hold times for the BMI160's flat detection algorithm
        /// </summary>
        public enum FlatHoldTime {
            _0ms,
            _512ms,
            _1024ms,
            _2048ms
        }
        /// <summary>
        /// Extension of the <see cref="ITapDataProducer"/> interface providing
        /// configuration options specific to the BMI160 IMU
        /// </summary>
        public interface IBma255FlatDataProducer : IFlatDataProducer {
            /// <summary>
            /// Configure the flat detection algorithm.
            /// </summary>
            /// <param name="hold">Delay for which the flat value must remain stable for an interrupt</param>
            /// <param name="theta">Threshold angle defining a flat position, between [0, 44.8] degrees</param>
            void Configure(FlatHoldTime? hold = null, float? theta = null);
        }
    }

    /// <summary>
    /// Extension of the <see cref="IAccelerometer"/> interface providing finer control of the BMA255 accelerometer
    /// </summary>
    public interface IAccelerometerBma255 : IAccelerometerBosch {
        /// <summary>
        /// Async data producer for the BMA255's flat detection algorithm
        /// </summary>
        new IBma255FlatDataProducer Flat { get; }

        /// <summary>
        /// Configure the snsor with settings specific to the BMA255 accelerometer
        /// </summary>
        /// <param name="odr">Output data rate</param>
        /// <param name="range">Data range</param>
        void Configure(OutputDataRate odr = OutputDataRate._125Hz, DataRange range = DataRange._2g);
    }
}
