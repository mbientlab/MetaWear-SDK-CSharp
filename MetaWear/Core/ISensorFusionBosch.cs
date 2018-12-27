using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Core.SensorFusionBosch;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace MbientLab.MetaWear.Core {
    namespace SensorFusionBosch {
        /// <summary>
        /// Supported sensor fusion modes
        /// </summary>
        public enum Mode {
            Ndof,
            ImuPlus,
            Compass,
            M4g
        }
        /// <summary>
        /// Supported data ranges for acceleration data
        /// </summary>
        public enum AccRange {
            _2g,
            _4g,
            _8g,
            _16g
        }
        /// <summary>
        /// Supported data ranges for gyro data
        /// </summary>
        public enum GyroRange {
            _2000dps,
            _1000dps,
            _500dps,
            _250dps
        }
        /// <summary>
        /// Accuracy of the correct sensor data
        /// </summary>
        public enum CalibrationAccuracy {
            Unreliable,
            LowAccuracy,
            MediumAccuracy,
            HighAccuracy
        }
        /// <summary>
        /// Container class holding corrected acceleration data, in units of g's
        /// </summary>
        public class CorrectedAcceleration : Acceleration {
            private byte accuracy;

            public CalibrationAccuracy Accuracy {
                get {
                    return (CalibrationAccuracy) accuracy;
                }
            }

            public CorrectedAcceleration(float x, float y, float z, byte accuracy) : base(x, y, z) {
                this.accuracy = accuracy;
            }

            public override string ToString() {
                return string.Format("{{X: {0:F3}g, Y: {1:F3}g, Z: {2:F3}g, Accuracy: {4}{3}", X, Y, Z, "}", accuracy);
            }

            public override bool Equals(Object obj) {
                if(this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;
                if (!base.Equals(obj)) return false;

                CorrectedAcceleration that = (CorrectedAcceleration) obj;

                return accuracy == that.accuracy;
            }

            public override int GetHashCode() {
                int result = base.GetHashCode();
                result = 31 * result + accuracy.GetHashCode();
                return result;
            }
        }
        /// <summary>
        /// Container class holding corrected angular velocity data, in degrees per second
        /// </summary>
        public class CorrectedAngularVelocity : AngularVelocity {
            private byte accuracy;

            public CalibrationAccuracy Accuracy {
                get {
                    return (CalibrationAccuracy)accuracy;
                }
            }

            public CorrectedAngularVelocity(float x, float y, float z, byte accuracy) : base(x, y, z) {
                this.accuracy = accuracy;
            }

            public override string ToString() {
                var dps = string.Format("{0}/s", DEGREES.ToString());
                return string.Format("{{X: {0:F3}{4}, Y: {1:F3}{5}, Z: {2:F3}{6}{3}, Accuracy: {7}", X, Y, Z, "}", dps, dps, dps, accuracy);
            }

            public override bool Equals(Object obj) {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;
                if (!base.Equals(obj)) return false;

                CorrectedAngularVelocity that = (CorrectedAngularVelocity)obj;

                return accuracy == that.accuracy;
            }

            public override int GetHashCode() {
                int result = base.GetHashCode();
                result = 31 * result + accuracy.GetHashCode();
                return result;
            }
        }
        /// <summary>
        /// Container class holding corrected magnetic field strength data, in Tesla
        /// </summary>
        public class CorrectedMagneticField : AngularVelocity {
            private byte accuracy;

            public CalibrationAccuracy Accuracy {
                get {
                    return (CalibrationAccuracy) accuracy;
                }
            }

            public CorrectedMagneticField(float x, float y, float z, byte accuracy) : base(x, y, z) {
                this.accuracy = accuracy;
            }

            public override string ToString() {
                return string.Format("{{X: {0:F9}T, Y: {1:F9}T, Z: {2:F9}T, Accuracy: {4}{3}", X, Y, Z, "}", accuracy);
            }

            public override bool Equals(Object obj) {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;
                if (!base.Equals(obj)) return false;

                CorrectedMagneticField that = (CorrectedMagneticField)obj;

                return accuracy == that.accuracy;
            }

            public override int GetHashCode() {
                int result = base.GetHashCode();
                result = 31 * result + accuracy.GetHashCode();
                return result;
            }
        }
        /// <summary>
        /// Container class holding the calibration state of the IMU sensors
        /// </summary>
        public struct ImuCalibrationState {
            /// <summary>
            /// Current calibration accuracy values for the accelerometer, gyroscope, and magnetometer respectively
            /// </summary>
            public readonly CalibrationAccuracy accelerometer, gyroscope, magnetometer;

            public ImuCalibrationState(CalibrationAccuracy accelerometer, CalibrationAccuracy gyroscope, CalibrationAccuracy magnetometer) {
                this.accelerometer = accelerometer;
                this.gyroscope = gyroscope;
                this.magnetometer = magnetometer;
            }

            public override bool Equals(object obj) {
                if (!(obj is ImuCalibrationState)) {
                    return false;
                }

                var state = (ImuCalibrationState)obj;
                return accelerometer == state.accelerometer &&
                       gyroscope == state.gyroscope &&
                       magnetometer == state.magnetometer;
            }

            public override int GetHashCode() {
                var hashCode = -56290531;
                hashCode = hashCode * -1521134295 + accelerometer.GetHashCode();
                hashCode = hashCode * -1521134295 + gyroscope.GetHashCode();
                hashCode = hashCode * -1521134295 + magnetometer.GetHashCode();
                return hashCode;
            }

            public override string ToString() {
                return string.Format("{{accelerometer: {0}, gyroscope: {1}, magnetometer: {2}{3}", 
                    Enum.GetName(typeof(CalibrationAccuracy), accelerometer),
                    Enum.GetName(typeof(CalibrationAccuracy), gyroscope),
                    Enum.GetName(typeof(CalibrationAccuracy), magnetometer), "}");
            }
        }
        /// <summary>
        /// Container class holding the IMU calibration data
        /// </summary>
        public struct ImuCalibrationData {
            /// <summary>
            /// Current calibration accuracy values for the accelerometer, gyroscope, and magnetometer respectively
            /// </summary>
            public readonly byte[] accelerometer, gyroscope, magnetometer;

            public ImuCalibrationData(byte[] accelerometer, byte[] gyroscope, byte[] magnetometer) {
                this.accelerometer = accelerometer;
                this.gyroscope = gyroscope;
                this.magnetometer = magnetometer;
            }

            public override bool Equals(object obj) {
                if (!(obj is ImuCalibrationData)) {
                    return false;
                }

                var state = (ImuCalibrationData)obj;
                return (accelerometer != null && state.accelerometer != null && accelerometer.SequenceEqual(state.accelerometer)) &&
                       (gyroscope != null && state.gyroscope != null && gyroscope.SequenceEqual(state.gyroscope)) &&
                       (magnetometer != null && state.magnetometer != null && magnetometer.SequenceEqual(state.magnetometer));
            }

            public override int GetHashCode() {
                (int, int) hash((int, int) acc, byte e) {
                    var i = acc.Item2;
                    return (acc.Item1 | (e << i), i + 1);
                }
                var hashCode = -56290531;
                hashCode = hashCode * -1521134295 + (accelerometer == null ? 0 : accelerometer.Take(4).Aggregate((0, 0), hash).Item1);
                hashCode = hashCode * -1521134295 + (gyroscope == null ? 0 : gyroscope.Take(4).Aggregate((0, 0), hash).Item1);
                hashCode = hashCode * -1521134295 + (magnetometer == null ? 0 : magnetometer.Take(4).Aggregate((0, 0), hash).Item1);
                return hashCode;
            }

            public override string ToString() {
                var acc = accelerometer == null ? "[]" : string.Format("[0x{0}]", BitConverter.ToString(accelerometer).ToLower().Replace("-", ", 0x"));
                var gyr = gyroscope == null ? "[]" : string.Format("[0x{0}]", BitConverter.ToString(gyroscope).ToLower().Replace("-", ", 0x"));
                var mag = gyroscope == null ? "[]" : string.Format("[0x{0}]", BitConverter.ToString(magnetometer).ToLower().Replace("-", ", 0x"));
                return string.Format("{{accelerometer: {0}, gyroscope: {1}, magnetometer: {2}{3}", acc, gyr, mag, "}");
            }
        }
    }
    /// <summary>
    /// Bosch algorithm combining accelerometer, gyroscope, and magnetometer data for Bosch sensors.  
    /// <para>When using sensor fusion, do not configure the accelerometer, gyro, and magnetometer with 
    /// their respective interface; the algorithm will automatically configure those sensors based on 
    /// the selected fusion mode.</para>
    /// </summary>
    public interface ISensorFusionBosch : IModule {
        /// <summary>
        /// Data producer representing corrected acceleration data
        /// </summary>
        IAsyncDataProducer CorrectedAcceleration { get; }
        /// <summary>
        /// Data producer representing corrected angular velocity data
        /// </summary>
        IAsyncDataProducer CorrectedAngularVelocity { get; }
        /// <summary>
        /// Data producer representing corrected magnetic field data
        /// </summary>
        IAsyncDataProducer CorrectedMagneticField { get; }
        /// <summary>
        /// Data producer representing quaternion data
        /// </summary>
        IAsyncDataProducer Quaternion { get; }
        /// <summary>
        /// Data producer representing Euler angles data
        /// </summary>
        IAsyncDataProducer EulerAngles { get; }
        /// <summary>
        /// Data producer representing gravity data
        /// </summary>
        IAsyncDataProducer Gravity { get; }
        /// <summary>
        /// Data producer representing linear acceleration data
        /// </summary>
        IAsyncDataProducer LinearAcceleration { get; }

        /// <summary>
        /// Configure the sensor fusion algorithm
        /// </summary>
        /// <param name="mode">Sensor fusion mode</param>
        /// <param name="ar">Accelerometer data range</param>
        /// <param name="gr">Gyro data range</param>
        /// <param name="accExtra">Extra configuration settings for the accelerometer</param>
        /// <param name="gyroExtra">Extra configuration settings for the gyro</param>
        void Configure(Mode mode = Mode.Ndof, AccRange ar = AccRange._16g, GyroRange gr = GyroRange._2000dps,
            object[] accExtra = null, object[] gyroExtra = null);

        /// <summary>
        /// Start the algorithm
        /// </summary>
        void Start();
        /// <summary>
        /// Stop the algorithm
        /// </summary>
        void Stop();

        /// <summary>
        /// Pulls the current sensor fusion configuration from the sensor
        /// </summary>
        /// <returns>Task that is completed when the settings are received</returns>
        Task PullConfigAsync();
        /// <summary>
        /// Reads the current calibration state from the sensor fusion algorithm.  This function cannot be
        /// called until the sensor fusion algorithm is running and is only available on firmware v1.4.1+
        /// </summary>
        /// <returns>Current calibrartion state</returns>
        /// <exception cref="InvalidOperationException">If device is not using min required firmware</exception>
        Task<ImuCalibrationState> ReadCalibrationStateAsync();

        /// <summary>
        /// Convenience method to poll the calibration state until the required IMUs are in a high accuracy state
        /// </summary>
        /// <param name="ct">The cancellation token that will be checked before reading the calibration state</param>
        /// <param name="pollingPeriod">How frequently poll the calibration state in milliseconds, defaults to 1000ms</param>
        /// <param name="progress">Handler for calibration state updates</param>
        /// <returns>IMU calibration data when task is completed, used with <see cref="WriteCalibrationData(ImuCalibrationData)"/></returns>
        /// <exception cref="InvalidOperationException">If device is not running firmware v1.4.3+</exception>
        /// <exception cref="TimeoutException">Timeout limit hit before task completed</exception>
        Task<ImuCalibrationData> Calibrate(CancellationToken ct, int pollingPeriod = 1000, Action<ImuCalibrationState> progress = null);
        /// <summary>
        /// Writes calibration data to the sensor fusion algorithm.  Combine this with <see cref="IMacro"/> module to 
        /// write thr data at boot time.
        /// </summary>
        /// <param name="data">Calibration data returned from the <see cref="Calibrate"/> function</param>
        void WriteCalibrationData(ImuCalibrationData data);
    }
}