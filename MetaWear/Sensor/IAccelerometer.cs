namespace MbientLab.MetaWear.Sensor {
    /// <summary>
    /// Measures sources of acceleration, such as gravity or motion.  This interface only provides general
    /// access to an accelerometer.  If you know specifically which accelerometer is on your board, use the
    /// appropriate subclass instead.
    /// <seealso cref="IAccelerometerBma255"/>
    /// <seealso cref="IAccelerometerBmi160"/>
    /// <seealso cref="IAccelerometerMma8452q"/>
    /// </summary>
    public interface IAccelerometer : IModule {
        /// <summary>
        /// Data producer representing the sensor's acceleration data
        /// </summary>
        IAsyncDataProducer Acceleration { get; }
        /// <summary>
        /// Variant data producer that packs 3 acceleration samples in to 1 ble packet.  
        /// Only streaming is supported by this data producer
        /// </summary>
        IAsyncDataProducer PackedAcceleration { get; }

        /// <summary>
        /// General function to configure the accelerometer.  The closest valid values will be selected
        /// based on the underlying accelerometer which may not equal the input values.
        /// </summary>
        /// <param name="odr">Output data rate, defaults to 100Hz</param>
        /// <param name="range">Acceleration data range, defaults to 2g</param>
        void Configure(float odr = 100f, float range = 2f);
        /// <summary>
        /// Switch the accelerometer into active mode
        /// </summary>
        void Start();
        /// <summary>
        /// Switch the accelerometer into standby mode
        /// </summary>
        void Stop();
    }
}
