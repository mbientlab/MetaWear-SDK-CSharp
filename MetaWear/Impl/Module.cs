using System.Collections.Generic;

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

    internal class Modules {
        static readonly internal Dictionary<Module, string> FriendlyNames = new Dictionary<Module, string> {
            { Module.SWITCH, "Switch" },
            { Module.LED, "Led" },
            { Module.ACCELEROMETER, "Accelerometer" },
            { Module.TEMPERATURE, "Temperature" },
            { Module.GPIO, "Gpio" },
            { Module.NEO_PIXEL, "NeoPixel" },
            { Module.IBEACON, "IBeacon" },
            { Module.HAPTIC, "Haptic" },
            { Module.DATA_PROCESSOR, "DataProcessor" },
            { Module.EVENT, "Event" },
            { Module.LOGGING, "Logging" },
            { Module.TIMER, "Timer" },
            { Module.SERIAL_PASSTHROUGH, "SerialPassthrough" },
            { Module.MACRO, "Macro" },
            { Module.GSR, "Conductance" },
            { Module.SETTINGS, "Settings" },
            { Module.BAROMETER, "Barometer" },
            { Module.GYRO, "Gyro" },
            { Module.AMBIENT_LIGHT, "AmbientLight" },
            { Module.MAGNETOMETER, "Magnetometer" },
            { Module.HUMIDITY, "Humidity" },
            { Module.COLOR_DETECTOR, "Color" },
            { Module.PROXIMITY, "Proximity" },
            { Module.SENSOR_FUSION, "SensorFusion" },
            { Module.DEBUG, "Debug" }
        };
    }
}
