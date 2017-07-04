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
    }
    /// <summary>
    /// Extension of the <see cref="IAccelerometer"/> providing general access to a Bosch accelerometer.  If you know specifically which
    /// Bosch accelerometer is on your board, use the appropriate subclass instead.
    /// <seealso cref="IAccelerometerBma255"/>
    /// <seealso cref="IAccelerometerBmi160"/>
    /// </summary>
    public interface IAccelerometerBosch : IAccelerometer {
        
    }
}
