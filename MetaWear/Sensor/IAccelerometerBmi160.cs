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
        /// <summary>
        /// Operation modes for the step detector algorithm
        /// </summary>
        public enum StepDetectorMode {
            /// <summary>
            /// Default mode with a balance between false positives and false negatives
            /// </summary>
            Normal,
            /// <summary>
            /// For light weighted persons that gives few false negatives but eventually more false positives
            /// </summary>
            Sensitive,
            /// <summary>
            /// Gives few false positives but eventually more false negatives
            /// </summary>
            Robust
        }
        /// <summary>
        /// Accumulates the number of detected steps in a counter that will send its current value on request, cannot be used 
        /// in conjunction with the <see cref="IStepDetectorDataProducer"/> interface.
        /// </summary>
        public interface IStepCounterDataProducer : IForcedDataProducer {
            /// <summary>
            /// Configure the step counter algorithm.  Must be called to have the step algorithm function as a counter.
            /// </summary>
            /// <param name="mode">Sensitivity mode, defaults to <see cref="StepDetectorMode.Normal"/></param>
            void Configure(StepDetectorMode mode = StepDetectorMode.Normal);
            /// <summary>
            /// Resets the internal step counter
            /// </summary>
            void Reset();
        }
        /// <summary>
        /// Interrupt driven step detection where each detected step triggers a data interrupt, cannot be used in 
        /// conjunction with the <see cref="IStepCounterDataProducer"/> interface.
        /// </summary>
        public interface IStepDetectorDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the step counter algorithm.  Must be called to have the step algorithm function as a detector.
            /// </summary>
            /// <param name="mode">Sensitivity mode, defaults to <see cref="StepDetectorMode.Normal"/></param>
            void Configure(StepDetectorMode mode = StepDetectorMode.Normal);
        }
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometerBosch"/> interface providing finer control of the BMI160 accelerometer features
    /// </summary>
    public interface IAccelerometerBmi160 : IAccelerometerBosch {
        /// <summary>
        /// Gets the data producer for the step counter output
        /// </summary>
        IStepCounterDataProducer StepCounter { get; }
        /// <summary>
        /// Gets the data producer for the step detector output
        /// </summary>
        IStepDetectorDataProducer StepDetector { get; }
        
        /// <summary>
        /// Configure the snsor with settings specific to the BMA255 accelerometer
        /// </summary>
        /// <param name="odr">Output data rate</param>
        /// <param name="range">Data range</param>
        void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g);
    }
}
