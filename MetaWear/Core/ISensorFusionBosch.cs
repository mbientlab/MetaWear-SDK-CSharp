using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Core.SensorFusionBosch;
using System;
using System.Threading.Tasks;

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
        /// <param name="accGyro">Extra configuration settings for the gyro</param>
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
    }
}
