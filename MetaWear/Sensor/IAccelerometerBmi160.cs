using MbientLab.MetaWear.Sensor.AccelerometerBmi160;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Sensor {
    namespace AccelerometerBmi160 {
        /// <summary>
        /// Accelerometer digital filter modes on the BMI160
        /// </summary>
        public enum FilterMode {
            Osr4,
            Osr2,
            Normal
        }
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

        /// <summary>
        /// Enumeration of hold times for the BMI160's flat detection algorithm
        /// </summary>
        public enum FlatHoldTime {
            _0ms,
            _640ms,
            _1280ms,
            _2560ms
        }
        /// <summary>
        /// Extension of the <see cref="ITapDataProducer"/> interface providing
        /// configuration options specific to the BMI160 IMU
        /// </summary>
        public interface IBmi160FlatDataProducer : IFlatDataProducer {
            /// <summary>
            /// Configure the flat detection algorithm.
            /// </summary>
            /// <param name="hold">Delay for which the flat value must remain stable for an interrupt</param>
            /// <param name="theta">Threshold angle defining a flat position, between [0, 44.8] degrees</param>
            void Configure(FlatHoldTime? hold = null, float? theta = null);
        }

        /// <summary>
        /// Skip times available for significant motion detection
        /// </summary>
        public enum SkipTime {
            /// <summary>
            /// 1.5 seconds
            /// </summary>
            _1_5s,
            _3s,
            _6s,
            _12s
        }
        /// <summary>
        /// Proof times available for significant motion detection
        /// </summary>
        public enum ProofTime {
            /// <summary>
            /// 0.25 seconds
            /// </summary>
            _0_25s,
            /// <summary>
            /// 0.5 seconds
            /// </summary>
            _0_5s,
            _1s,
            _2s
        }
        /// <summary>
        /// Extension of the <see cref="IMotionDataProducer"/> interface providing configuration 
        /// options for significant motion detection on the BMI160 IMU
        /// </summary>
        public interface IBmi160MotionDataProducer : IMotionDataProducer {
            /// <summary>
            /// Configure the accelerometer for significant-motion detection
            /// </summary>
            /// <param name="skip">Number of seconds to sleep after movement is detected</param>
            /// <param name="proof">Number of seconds that movement must still be detected after the skip time passed</param>
            void ConfigureSignificant(SkipTime? skip = null, ProofTime? proof = null);
        }
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometerBosch"/> interface providing finer control of the BMI160 accelerometer features
    /// </summary>
    public interface IAccelerometerBmi160 : IAccelerometerBosch {
        /// <summary>
        /// Data producer representing the step counter output
        /// </summary>
        IStepCounterDataProducer StepCounter { get; }
        /// <summary>
        /// Data producer representing the step detector output
        /// </summary>
        IStepDetectorDataProducer StepDetector { get; }
        /// <summary>
        /// Async data producer for the BMI160's flat detection algorithm
        /// </summary>
        new IBmi160FlatDataProducer Flat { get; }
        /// <summary>
        /// Async data producer for the BMI160's motion detection algorithm
        /// </summary>
        new IBmi160MotionDataProducer Motion { get; }

        /// <summary>
        /// Configure the snsor with settings specific to the BMA255 accelerometer
        /// </summary>
        /// <param name="odr">Output data rate, defaults to 100Hz</param>
        /// <param name="range">Data range, defaults to +/-2g</param>
        /// <param name="filter">Accelerometer digital filter mode, defaults to <see cref="FilterMode.Normal"/></param>
        void Configure(OutputDataRate odr = OutputDataRate._100Hz, DataRange range = DataRange._2g, FilterMode filter = FilterMode.Normal);
    }
}
