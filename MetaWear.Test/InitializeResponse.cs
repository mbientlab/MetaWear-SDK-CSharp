using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Sensor;

using System;
using System.Collections.Generic;
using System.Text;

namespace MbientLab.MetaWear.Test {
    class InitializeResponse {
        static readonly Dictionary<Type, byte[]> MODULE_RESPONSE = new Dictionary<Type, byte[]>();

        static InitializeResponse() {
            MODULE_RESPONSE.Add(typeof(ISwitch), new byte[] {0x01, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(ILed), new byte[] {0x02, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IAccelerometerBma255), new byte[] {0x03, 0x80, 0x03, 0x01});
            MODULE_RESPONSE.Add(typeof(IAccelerometerBmi160), new byte[] {0x03, 0x80, 0x01, 0x01});
            MODULE_RESPONSE.Add(typeof(IAccelerometerMma8452q), new byte[] {0x03, 0x80, 0x00, 0x01});
            MODULE_RESPONSE.Add(typeof(ITemperature), new byte[] {0x04, 0x80, 0x01, 0x00, 0x00, 0x03, 0x01, 0x02});
            MODULE_RESPONSE.Add(typeof(IGpio), new byte[] {0x05, 0x80, 0x00, 0x02, 0x03, 0x03, 0x03, 0x03, 0x01, 0x01, 0x01, 0x01});
            MODULE_RESPONSE.Add(typeof(INeoPixel), new byte[] {0x06, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IIBeacon), new byte[] {0x07, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IHaptic), new byte[] {0x08, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IDataProcessor), new byte[] {0x09, 0x80, 0x00, 0x03, 0x1c});
            MODULE_RESPONSE.Add(typeof(ILogging), new byte[] {0x0b, 0x80, 0x00, 0x02, 0x08, 0x80, 0x2b, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(ISerialPassthrough), new byte[] {0x0d, 0x80, 0x00, 0x01});
            MODULE_RESPONSE.Add(typeof(IMacro), new byte[] {0x0f, 0x80, 0x00, 0x01, 0x08});
            MODULE_RESPONSE.Add(typeof(ISettings), new byte[] {0x11, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IBarometerBme280), new byte[] {0x12, 0x80, 0x01, 0x00});
            MODULE_RESPONSE.Add(typeof(IBarometerBmp280), new byte[] {0x12, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IGyroBmi160), new byte[] {0x13, 0x80, 0x00, 0x01});
            MODULE_RESPONSE.Add(typeof(IAmbientLightLtr329), new byte[] {0x14, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IMagnetometerBmm150), new byte[] {0x15, 0x80, 0x00, 0x01});
            MODULE_RESPONSE.Add(typeof(IHumidityBme280), new byte[] {0x16, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IColorTcs34725), new byte[] {0x17, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(IProximityTsl2671), new byte[] {0x18, 0x80, 0x00, 0x00});
            MODULE_RESPONSE.Add(typeof(ISensorFusionBosch), new byte[] {0x19, 0x80, 0x00, 0x00, 0x03, 0x00, 0x06, 0x00, 0x02, 0x00, 0x01, 0x00});
            MODULE_RESPONSE.Add(typeof(IDebug), new byte[] {0xfe, 0x80, 0x00, 0x00});
        }

        private static Dictionary<byte, byte[]> CreateModuleResponses(params Type[] moduleTypes) {
            var responses = new Dictionary<byte, byte[]>();
            bool hasDebug = false;

            if (moduleTypes != null) {
                foreach (Type type in moduleTypes) {
                    byte[] response;

                    if (MODULE_RESPONSE.TryGetValue(type, out response)) {
                        byte[] copy = new byte[response.Length];
                        Array.Copy(response, copy, copy.Length);
                        responses.Add(response[0], copy);
                    }
                    hasDebug |= type.Equals(typeof(IDebug));
                }
            }

            for (byte i = 1; i <= 0x19; i++) {
                if (i == 0xa) {
                    responses.Add(0xa, new byte[] { 0x0a, 0x80, 0x00, 0x00, 0x1C });
                } else if (i == 0xc) {
                    responses.Add(0xc, new byte[] { 0x0c, 0x80, 0x00, 0x00, 0x08 });
                } else if (!responses.ContainsKey(i)) {
                    responses.Add(i, new byte[] { i, 0x80 });
                }
            }

            if (!hasDebug) {
                responses.Add(0xfe, new byte[] { 0xfe, 0x80 });
            }

            return responses;
        }

        internal readonly byte[] modelNumber, hardwareRevision, manufacturer, serialNumber, firmwareRevision;
        internal readonly Dictionary<byte, byte[]> moduleResponses;

        private InitializeResponse(String modelNumber, String hardwareRevision, String firmwareRevision) {
            this.modelNumber = Encoding.ASCII.GetBytes(modelNumber);
            this.hardwareRevision = Encoding.ASCII.GetBytes(hardwareRevision);
            this.firmwareRevision = Encoding.ASCII.GetBytes(firmwareRevision);

            manufacturer = new byte[] { 0x4d, 0x62, 0x69, 0x65, 0x6e, 0x74, 0x4c, 0x61, 0x62, 0x20, 0x49, 0x6e, 0x63 };
            serialNumber = new byte[] { 0x30, 0x30, 0x33, 0x42, 0x46, 0x39 };
        }

        internal InitializeResponse(params Type[] moduleTypes) : this("1.2.5", moduleTypes) {  }

        internal InitializeResponse(string firmware, params Type[] moduleTypes) : this("deadbeef", "0.3", firmware) {
            moduleResponses = CreateModuleResponses(moduleTypes);
        }

        internal InitializeResponse(Model model, string firmware = "1.2.5") {
            hardwareRevision = Encoding.ASCII.GetBytes("0.1");
            firmwareRevision = Encoding.ASCII.GetBytes(firmware);

            switch (model) {
                case Model.MetaWearR:
                    modelNumber = Encoding.ASCII.GetBytes("0");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerMma8452q),
                        typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IDebug));
                    moduleResponses[4] = new byte[] { 0x04, 0x80, 0x01, 0x00, 0x00, 0x01 };
                    break;
                case Model.MetaWearRG:
                    modelNumber = Encoding.ASCII.GetBytes("1");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IGyroBmi160), typeof(IDebug));
                    break;
                case Model.MetaWearRPro:
                    modelNumber = Encoding.ASCII.GetBytes("1");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IBarometerBmp280), 
                        typeof(IGyroBmi160), typeof(IAmbientLightLtr329), typeof(IDebug));
                    break;
                case Model.MetaWearC:
                    modelNumber = Encoding.ASCII.GetBytes("2");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IGyroBmi160), typeof(IDebug));
                    break;
                case Model.MetaWearCPro:
                    modelNumber = Encoding.ASCII.GetBytes("2");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IBarometerBmp280),
                        typeof(IGyroBmi160), typeof(IAmbientLightLtr329), typeof(IMagnetometerBmm150), typeof(IDebug));
                    break;
                case Model.MetaEnv:
                    modelNumber = Encoding.ASCII.GetBytes("2");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBma255),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IBarometerBme280), 
                        typeof(IHumidityBme280), typeof(IColorTcs34725), typeof(IDebug));
                    break;
                case Model.MetaDetect:
                    modelNumber = Encoding.ASCII.GetBytes("2");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBma255),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IAmbientLightLtr329),
                        typeof(IProximityTsl2671), typeof(IDebug));
                    break;
                case Model.MetaHealth:
                    modelNumber = Encoding.ASCII.GetBytes("3");
                    // Fill this in later
                    moduleResponses = CreateModuleResponses(typeof(IDataProcessor), typeof(ILogging));
                    break;
                case Model.MetaTracker:
                    modelNumber = Encoding.ASCII.GetBytes("4");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IBarometerBme280),
                        typeof(IGyroBmi160), typeof(IAmbientLightLtr329), typeof(IHumidityBme280), typeof(IDebug));
                    break;
                case Model.MetaMotionR:
                    modelNumber = Encoding.ASCII.GetBytes("5");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IBarometerBmp280),
                        typeof(IGyroBmi160), typeof(IAmbientLightLtr329), typeof(IMagnetometerBmm150), typeof(ISensorFusionBosch), typeof(IDebug));
                    break;
                case Model.MetaMotionC:
                    modelNumber = Encoding.ASCII.GetBytes("6");
                    moduleResponses = CreateModuleResponses(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160),
                        typeof(ITemperature), typeof(IGpio), typeof(INeoPixel), typeof(IIBeacon), typeof(IHaptic),
                        typeof(IDataProcessor), typeof(ILogging), typeof(ISerialPassthrough),
                        typeof(IMacro), typeof(ISettings), typeof(IBarometerBmp280),
                        typeof(IGyroBmi160), typeof(IAmbientLightLtr329), typeof(IMagnetometerBmm150), typeof(ISensorFusionBosch), typeof(IDebug));
                    break;
            }
        }
    }
}
