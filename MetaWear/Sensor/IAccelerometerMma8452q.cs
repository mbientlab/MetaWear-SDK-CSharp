using MbientLab.MetaWear.Data;
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
        /// <summary>
        /// Available oversampling modes on the MMA8452Q sensor
        /// </summary>
        public enum Oversampling {
            Normal,
            LowNoiseLowPower,
            HighRes,
            LowPower
        }
        /// <summary>
        /// On-board algorithm that detects changes in the sensor's orientation.
        /// <para>Data is represented as a <see cref="SensorOrientation"/> enum.</para>
        /// </summary>
        public interface IOrientationDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the orientation detection algorithm
            /// </summary>
            /// <param name="delay">Time, in milliseconds, for which the sensor's orientation must remain in the new position 
            /// before a position change is triggered</param>
            void Configure(int delay = 100);
        }
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometer"/> interface providing finer control of the MMA8452Q accelerometer
    /// </summary>
    public interface IAccelerometerMma8452q : IAccelerometer {
        /// <summary>
        /// Async data producer for the orientation detection algorithm
        /// </summary>
        IOrientationDataProducer Orientation { get; }

        /// <summary>
        /// Configure the snsor with settings specific to the MMA8452Q accelerometer
        /// </summary>
        /// <param name="odr">Output data rate, defaults to 100Hz</param>
        /// <param name="range">Data range, defaults to +/-2g</param>
        /// <param name="highPassCutoff">Enables high pass filter with a cutoff frequency between [0.031, 16.0]Hz, defaults to disabled</param>
        /// <param name="oversample">New oversampling mode, defaults to <see cref="Oversampling.Normal"/></param>
        void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g, float? highPassCutoff = null,
                Oversampling oversample = Oversampling.Normal);
    }
}
