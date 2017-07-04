namespace MbientLab.MetaWear.Impl {
    internal enum Module : byte {
        SWITCH = 0x01,
        LED,
        ACCELEROMETER,
        TEMPERATURE,
        GPIO,
        NEO_PIXEL,
        IBEACON,
        HAPTIC,
        DATA_PROCESSOR,
        EVENT,
        LOGGING,
        TIMER,
        SERIAL_PASSTHROUGH,
        MACRO = 0x0f,
        GSR,
        SETTINGS,
        BAROMETER,
        GYRO,
        AMBIENT_LIGHT,
        MAGNETOMETER,
        HUMIDITY,
        COLOR_DETECTOR,
        PROXIMITY,
        SENSOR_FUSION,
        DEBUG = 0xfe
    }
}
