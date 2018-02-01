using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Sensor {
    namespace AccelerometerBosch {
        /// <summary>
        /// Available data ranges for Bosch accelerometers
        /// </summary>
        public enum DataRange {
            _2g,
            _4g,
            _8g,
            _16g
        }

        /// <summary>
        /// Calculation modes controlling the conditions that determine the sensor's orientation
        /// </summary>
        public enum OrientationMode {
            /// <summary>
            /// Default mode
            /// </summary>
            Symmetrical,
            HighAsymmetrical,
            LowAsymmetrical
        }
        /// <summary>
        /// On-board algorithm that detects changes in the sensor's orientation.
        /// <para>Data is represented as a <see cref="SensorOrientation"/> enum.</para>
        /// </summary>
        public interface IOrientationDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the orientation detection algorithm
            /// </summary>
            /// <param name="mode">New calculation mode</param>
            /// <param name="hysteresis">New hysteresis value for landscape/portrait detection.  
            /// This value is in g's and defaults to 0.0625g</param>
            void Configure(OrientationMode? mode = null, float? hysteresis = null);
        }

        /// <summary>
        /// On-board algorithm that detects whether or not the senor is laying flat.  
        /// <para>Data is represented as a boolean.</para>
        /// </summary>
        public interface IFlatDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the flat detection algorithm.
            /// </summary>
            /// <param name="hold">Delay for which the flat value must remain stable for an interrupt, in milliseconds.  
            /// The closest valid value will be chosen.</param>
            /// <param name="theta">Threshold angle defining a flat position, between [0, 44.8] degrees</param>
            void Configure(ushort? hold = null, float? theta = null);
        }

        /// <summary>
        /// Wrapper class encapsulating the response from low/high-g detecton
        /// </summary>
        public class LowHighG {
            /// <summary>
            /// True if the interrupt from from low-g motion
            /// </summary>
            public bool IsLow { get; }
            /// <summary>
            /// True if the interrupt from from high-g motion.  If it is not high-g motion, there is no
            /// need to check the high-g variables
            /// </summary>
            public bool IsHigh { get; }
            /// <summary>
            /// True if the x-axis triggered high-g interrupt
            /// </summary>
            public bool HighGx { get; }
            /// <summary>
            /// True if the y-axis triggered high-g interrupt
            /// </summary>
            public bool HighGy { get; }
            /// <summary>
            /// True if the z-axis triggered high-g interrupt
            /// </summary>
            public bool HighGz { get; }
            /// <summary>
            /// Direction of the high-g motion interrupt
            /// </summary>
            public Sign HighSign { get; }

            public LowHighG(bool isHigh, bool isLow, bool highGx, bool highGy, bool highGz, Sign highSign) {
                IsHigh = isHigh;
                IsLow = isLow;
                HighGx = highGx;
                HighGy = highGy;
                HighGz = highGz;
                HighSign = highSign;
            }

            public override bool Equals(object obj) {
                var high = obj as LowHighG;
                return high != null &&
                       IsLow == high.IsLow &&
                       IsHigh == high.IsHigh &&
                       HighGx == high.HighGx &&
                       HighGy == high.HighGy &&
                       HighGz == high.HighGz &&
                       HighSign == high.HighSign;
            }

            public override int GetHashCode() {
                var hashCode = -240861675;
                hashCode = hashCode * -1521134295 + IsLow.GetHashCode();
                hashCode = hashCode * -1521134295 + IsHigh.GetHashCode();
                hashCode = hashCode * -1521134295 + HighGx.GetHashCode();
                hashCode = hashCode * -1521134295 + HighGy.GetHashCode();
                hashCode = hashCode * -1521134295 + HighGz.GetHashCode();
                hashCode = hashCode * -1521134295 + HighSign.GetHashCode();
                return hashCode;
            }

            public override string ToString() {
                return string.Format("{{low: {0}, high: {1}, high_x: {2}, high_y: {3}, high_z: {4}, high_sign: {5}{6}", 
                    IsLow, IsHigh, HighGx, HighGy, HighGz, HighSign, "}");
            }
        }
        /// <summary>
        /// Interrupt modes for low-g detection
        /// </summary>
        public enum LowGMode {
            /// <summary>
            ///  Compare |acc_x|, |acc_y|, |acc_z| with the low threshold
            /// </summary>
            Single,
            /// <summary>
            /// Compare |acc_x| + |acc_y| + |acc_z| with the low threshold
            /// </summary>
            Sum
        }
        /// <summary>
        /// On-board algorithm that detects when low (i.e. free fall) or high g acceleration is measured.  Data is 
        /// represented as a <see cref="LowHighG"/> object
        /// </summary>
        public interface ILowAndHighGDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the low/high-g detection algorithm.  
            /// <para>Both detection types are configured at once with this function.  All parameters are optional so 
            /// developers only need to set the ones they are need</para>
            /// </summary>
            /// <param name="enableLowG">Set to 'true' to enable low-g detection, defaults to 'false'</param>
            /// <param name="lowDuration">Minimum amount of time the acceleration must stay below (ths + hys) for an interrupt, between [2.5, 640]ms</param>
            /// <param name="lowThreshold">Threshold that triggers a low-g interrupt, between [0.00391, 2.0]g</param>
            /// <param name="lowHysteresis">Hysteresis level for low-g interrupt, between [0, 0.375]g</param>
            /// <param name="mode">Low G detection type</param>
            /// <param name="enableHighGx">Set to 'true' to enable high-g detection on the x-axis, default to 'false'</param>
            /// <param name="enableHighGy">Set to 'true' to enable high-g detection on the y-axis, default to 'false'</param>
            /// <param name="enableHighGz">Set to 'true' to enable high-g detection on the z-axis, default to 'false'</param>
            /// <param name="highDuration">Minimum amount of time the acceleration sign does not change for an interrupt</param>
            /// <param name="highThreshold">Threshold for clearing high-g interrupt</param>
            /// <param name="highHysteresis">Hysteresis level for clearing the high-g interrupt</param>
            void Configure(bool enableLowG = false, ushort? lowDuration = null, float? lowThreshold = null, float? lowHysteresis = null, LowGMode? mode = null,
                bool enableHighGx = false, bool enableHighGy = false, bool enableHighGz = false, ushort? highDuration = null, float? highThreshold = null, float? highHysteresis = null);
        }

        /// <summary>
        /// Wrapper class encapsulating responses from any motion detection
        /// </summary>
        public class AnyMotion {
            /// <summary>
            /// Slope sign of the triggering motion
            /// </summary>
            public Sign Sign { get; }
            /// <summary>
            /// True if x-axis triggered the motion interrupt
            /// </summary>
            public bool XAxisActive { get; }
            /// <summary>
            /// True if y-axis triggered the motion interrupt
            /// </summary>
            public bool YAxisActive { get; }
            /// <summary>
            /// True if z-axis triggered the motion interrupt
            /// </summary>
            public bool ZAxisActive { get; }

            public AnyMotion(Sign sign, bool xAxisActive, bool yAxisActive, bool zAxisActive) {
                Sign = sign;
                XAxisActive = xAxisActive;
                YAxisActive = yAxisActive;
                ZAxisActive = zAxisActive;
            }

            public override bool Equals(object obj) {
                var motion = obj as AnyMotion;
                return motion != null &&
                       Sign == motion.Sign &&
                       XAxisActive == motion.XAxisActive &&
                       YAxisActive == motion.YAxisActive &&
                       ZAxisActive == motion.ZAxisActive;
            }

            public override int GetHashCode() {
                var hashCode = -449418931;
                hashCode = hashCode * -1521134295 + Sign.GetHashCode();
                hashCode = hashCode * -1521134295 + XAxisActive.GetHashCode();
                hashCode = hashCode * -1521134295 + YAxisActive.GetHashCode();
                hashCode = hashCode * -1521134295 + ZAxisActive.GetHashCode();
                return hashCode;
            }

            public override string ToString() {
                return string.Format("{{sign: {0}, x-axis active: {1}, y-axis active: {2}, z-axis active: {3}{4}",
                    Sign, XAxisActive, YAxisActive, ZAxisActive, "}");
            }
        }
        /// <summary>
        /// On-board algorithms for detecting various types of motion.  
        /// <para>The various 'Configure' methods in this interface both enable and configure their respective motion detection types.  
        /// Only of these types can be detected at a time.</para>
        /// </summary>
        public interface IMotionDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the accelerometer for no-motion detection
            /// </summary>
            /// <param name="duration">Time, in milliseconds, for which no slope data points exceed the threshold</param>
            /// <param name="threshold">Threshold, in g's, for which no slope data points must exceed</param>
            void ConfigureNo(int? duration = null, float? threshold = null);
            /// <summary>
            /// Configure the accelerometer for any-motion detection
            /// </summary>
            /// <param name="count">Number of consecutive slope data points that must be above the threshold</param>
            /// <param name="threshold">Value that the slope data points must be above</param>
            void ConfigureAny(int? count = null, float? threshold = null);
            /// <summary>
            /// Configure the accelerometer for slow-motion detection
            /// </summary>
            /// <param name="count">Number of consecutive slope data points that must be above the threshold</param>
            /// <param name="threshold">Threshold, in g's, for which no slope data points must exceed</param>
            void ConfigureSlow(byte? count = null, float? threshold = null);
        }

        /// <summary>
        /// Wrapper class encapsulating responses from tap detection
        /// </summary>
        public class Tap { 
            public TapType? Type { get; }
            public Sign Sign { get; }

            public Tap(TapType? type, Sign sign) {
                Type = type;
                Sign = sign;
            }

            public override bool Equals(object obj) {
                var tap = obj as Tap;
                return tap != null &&
                       Type == tap.Type &&
                       Sign == tap.Sign;
            }

            public override int GetHashCode() {
                var hashCode = 159830841;
                hashCode = hashCode * -1521134295 + Type.GetHashCode();
                hashCode = hashCode * -1521134295 + Sign.GetHashCode();
                return hashCode;
            }

            public override string ToString() {
                return string.Format("{{type: {0}, sign: {1}{2}", Type, Sign, "}");
            }
        }
        /// <summary>
        /// Available quiet times for double tap detection
        /// </summary>
        public enum TapQuietTime {
            _30ms,
            _20ms
        }
        /// <summary>
        /// Available shock times for tap detection
        /// </summary>
        public enum TapShockTime {
            _50ms,
            _75ms
        }
        /// <summary>
        /// Available windows for double tap detection
        /// </summary>
        public enum DoubleTapWindow {
            _50ms,
            _100ms,
            _150ms,
            _200ms,
            _250ms,
            _375ms,
            _500ms,
            _700ms
        }
        /// <summary>
        /// Ob-board algorithm that detects taps.  Data is repesented as a <see cref="Tap"/> object.
        /// </summary>
        public interface ITapDataProducer : IAsyncDataProducer {
            /// <summary>
            /// Configure the tap detection
            /// </summary>
            /// <param name="enableSingle">Set to 'true' to enable single tap, defaults to 'false'</param>
            /// <param name="enableDouble">Set to 'true' to enable double tap, defaults to 'false'</param>
            /// <param name="threshold">Threshold that the acceleration difference must exceed for a tap, in g's</param>
            /// <param name="quiet">Time that must pass before a second tap can occur</param>
            /// <param name="shock">Time to lock the data in the status register</param>
            /// <param name="window">Length of time for a second shock to occur for a double tap</param>
            void Configure(bool enableSingle = false, bool enableDouble = false, float? threshold = null, TapQuietTime? quiet = null, TapShockTime? shock = null, DoubleTapWindow? window = null);
        }
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometer"/> providing general access to a Bosch accelerometer.  If you know specifically which
    /// Bosch accelerometer is on your board, use the appropriate subclass instead.
    /// <seealso cref="IAccelerometerBma255"/>
    /// <seealso cref="IAccelerometerBmi160"/>
    /// </summary>
    public interface IAccelerometerBosch : IAccelerometer {
        /// <summary>
        /// Async data producer for the orientation detection algorithm
        /// </summary>
        IOrientationDataProducer Orientation { get; }
        /// <summary>
        /// Async data producer for the flat detection algorithm
        /// </summary>
        IFlatDataProducer Flat { get; }
        /// <summary>
        /// Async data producer for the low/high-g detection algorithm
        /// </summary>
        ILowAndHighGDataProducer LowAndHighG { get; }
        /// <summary>
        /// Async data producer for the motion detection algorithm
        /// </summary>
        IMotionDataProducer Motion { get; }
        /// <summary>
        /// Async data producer for the tap detection algorithm
        /// </summary>
        ITapDataProducer Tap { get; }
    }
}
