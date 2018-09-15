using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Core.SensorFusionBosch;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.GyroBmi160;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class SensorFusionBoschTestDataClass {
        public static IEnumerable ConfigureTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach (var mode in Enum.GetValues(typeof(Mode))) {
                    foreach (var acc in Enum.GetValues(typeof(AccRange))) {
                        foreach (var gyr in Enum.GetValues(typeof(GyroRange))) {
                            foreach (var accFilter in Enum.GetValues(typeof(Sensor.AccelerometerBmi160.FilterMode))) {
                                foreach (var gyroFilter in Enum.GetValues(typeof(FilterMode))) {
                                    testCases.Add(new TestCaseData(mode, acc, gyr, accFilter, gyroFilter));
                                }
                            }
                        }
                    }
                }
                return testCases;
            }
        }

        public static IEnumerable StartAndStopTestCases {
            get {
                string[] producers = {
                   "CorrectedAcceleration",
                   "CorrectedAngularVelocity",
                   "CorrectedMagneticField",
                   "Quaternion",
                   "EulerAngles",
                   "Gravity",
                   "LinearAcceleration"
                };

                Tuple<int, byte[][]> command = Tuple.Create(11, new byte[][] {
                    new byte[] {0x19, 0x02, 0x01, 0x13},
                    new byte[] {0x03, 0x03, 0x28, 0x0c},
                    new byte[] {0x13, 0x03, 0x28, 0x00},
                    new byte[] {0x15, 0x04, 0x04, 0x0e},
                    new byte[] {0x15, 0x03, 0x6},
                    new byte[] {0x03, 0x02, 0x01, 0x00},
                    new byte[] {0x13, 0x02, 0x01, 0x00},
                    new byte[] {0x15, 0x02, 0x01, 0x00},
                    new byte[] {0x03, 0x01, 0x01},
                    new byte[] {0x13, 0x01, 0x01},
                    new byte[] {0x15, 0x01, 0x01},
                    new byte[] {0x19, 0x03, 0x00, 0x00},
                    new byte[] {0x19, 0x01, 0x01},
                    new byte[] {0x19, 0x01, 0x00},
                    new byte[] {0x19, 0x03, 0x00, 0x7f},
                    new byte[] {0x03, 0x01, 0x00},
                    new byte[] {0x13, 0x01, 0x00},
                    new byte[] {0x15, 0x01, 0x00},
                    new byte[] {0x03, 0x02, 0x00, 0x01},
                    new byte[] {0x13, 0x02, 0x00, 0x01},
                    new byte[] {0x15, 0x02, 0x00, 0x01}
                });

                List<TestCaseData> testCases = new List<TestCaseData>();
                int i = 0;
                foreach(var producer in producers) {
                    byte[][] copy = new byte[command.Item2.Length][];
                    for(int j = 0; j < command.Item2.Length; j++) {
                        copy[j] = new byte[command.Item2[j].Length];
                        Array.Copy(command.Item2[j], copy[j], copy[j].Length);
                    }

                    copy[command.Item1][2] |= (byte) (0x1 << i);
                    i++;

                    testCases.Add(new TestCaseData(producer, copy));
                }
                
                return testCases;
            }
        }

        public static IEnumerable InterpretDataTestCases {
            get {
                yield return new TestCaseData(
                    "CorrectedAcceleration",
                    new CorrectedAcceleration(
                        BitConverter.ToSingle(new byte[] { 0x00, 0x50, 0x58, 0xc0 }, 0), 
                        BitConverter.ToSingle(new byte[] { 0x00, 0xfe, 0x7f, 0x41 }, 0), 
                        BitConverter.ToSingle(new byte[] { 0x00, 0xfe, 0x7f, 0xc1 }, 0), 
                        0x00),
                    new byte[] { 0x19, 0x04, 0x20, 0x3e, 0x53, 0xc5, 0x0c, 0xfe, 0x79, 0x46, 0x0c, 0xfe, 0x79, 0xc6, 0x00 }
                );
                yield return new TestCaseData(
                    "CorrectedAngularVelocity",
                    new CorrectedAngularVelocity(
                        BitConverter.ToSingle(new byte[] { 0x7a, 0x56, 0x91, 0x42 }, 0),
                        BitConverter.ToSingle(new byte[] { 0xb4, 0x62, 0x60, 0xc2 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x73, 0x34, 0x04, 0x44 }, 0),
                        0x00),
                    new byte[] { 0x19, 0x05, 0x7a, 0x56, 0x91, 0x42, 0xb4, 0x62, 0x60, 0xc2, 0x73, 0x34, 0x04, 0x44, 0x00 }
                );
                yield return new TestCaseData(
                    "CorrectedMagneticField",
                    new CorrectedMagneticField(
                        BitConverter.ToSingle(new byte[] { 0x9c, 0x50, 0x08, 0x38 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x84, 0x4d, 0x78, 0xb7 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x44, 0x24, 0xf9, 0x37 }, 0),
                        0x03),
                    new byte[] { 0x19, 0x06, 0x00, 0x00, 0x02, 0x42, 0xcd, 0xcc, 0x6c, 0xc1, 0x9a, 0x99, 0xed, 0x41, 0x03 }
                );
                yield return new TestCaseData(
                    "Quaternion",
                    new Quaternion(
                        BitConverter.ToSingle(new byte[] { 0x1b, 0x9b, 0x70, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0x8c, 0x5e, 0x4d, 0xbd }, 0),
                        BitConverter.ToSingle(new byte[] { 0x07, 0x7f, 0x1d, 0xbe }, 0),
                        BitConverter.ToSingle(new byte[] { 0x78, 0x02, 0x9a, 0xbe }, 0)),
                    new byte[] { 0x19, 0x07, 0x1b, 0x9b, 0x70, 0x3f, 0x8c, 0x5e, 0x4d, 0xbd, 0x07, 0x7f, 0x1d, 0xbe, 0x78, 0x02, 0x9a, 0xbe }
                );
                yield return new TestCaseData(
                    "EulerAngles",
                    new EulerAngles(
                        BitConverter.ToSingle(new byte[] { 0xb1, 0xf9, 0xc5, 0x41 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x44, 0xb9, 0xf1, 0xc2 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x1a, 0x2f, 0x04, 0xc2 }, 0),
                        BitConverter.ToSingle(new byte[] { 0xb1, 0xf9, 0xc5, 0x41 }, 0)),
                    new byte[] { 0x19, 0x08, 0xb1, 0xf9, 0xc5, 0x41, 0x44, 0xb9, 0xf1, 0xc2, 0x1a, 0x2f, 0x04, 0xc2, 0xb1, 0xf9, 0xc5, 0x41 }
                );
                yield return new TestCaseData(
                    "Gravity",
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0xa8, 0x3b, 0x2c, 0x3d }, 0),
                        BitConverter.ToSingle(new byte[] { 0x25, 0x69, 0x53, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0x15, 0xdc, 0x0f, 0xbf }, 0)),
                    new byte[] { 0x19, 0x09, 0xee, 0x20, 0xd3, 0x3e, 0xb2, 0x93, 0x01, 0x41, 0x04, 0x59, 0xb0, 0xc0 }
                );
                yield return new TestCaseData(
                    "LinearAcceleration",
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0xf1, 0x8f, 0x97, 0x3e }, 0),
                        BitConverter.ToSingle(new byte[] { 0xe5, 0x39, 0xb8, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0xd2, 0x65, 0xc2, 0xbe }, 0)),
                    new byte[] { 0x19, 0x0a, 0x2f, 0xca, 0x39, 0x40, 0x86, 0xd4, 0x61, 0x41, 0x80, 0x4c, 0x6e, 0xc0 }
                );
            }
        }
    }

    abstract class SensorFusionBoschBaseTest : UnitTestBase {
        protected ISensorFusionBosch sensorFusion;

        public SensorFusionBoschBaseTest() : base(typeof(IAccelerometerBmi160), typeof(IGyroBmi160), typeof(IMagnetometerBmm150), typeof(ISensorFusionBosch)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            sensorFusion = metawear.GetModule<ISensorFusionBosch>();
        }

        private static readonly byte[] BMI160_ACC_RANGE_BITMASK = new byte[] { 0b0011, 0b0101, 0b1000, 0b1100 };
        private static readonly byte[][] CONFIG_MASKS = {
            new byte[] {0x10, 0x20, 0x30, 0x40},
            new byte[] {0x11, 0x21, 0x31, 0x41},
            new byte[] {0x12, 0x22, 0x32, 0x42},
            new byte[] {0x13, 0x23, 0x33, 0x43}
        };

        [Parallelizable]
        [TestCaseSource(typeof(SensorFusionBoschTestDataClass), "ConfigureTestCases")]
        public void Configure(Mode mode, AccRange acc, GyroRange gyr, Sensor.AccelerometerBmi160.FilterMode accFilter, FilterMode gyroFilter) {
            byte[][] expected = null;
            byte[] configGyro100Hz = new byte[] { 0x13, 0x03, (byte)(((int)gyroFilter << 4) | GyroBmi160Test.ODR_BITMASK[(int)OutputDataRate._100Hz]), GyroBmi160Test.RANGE_BITMASK[(int)gyr] };

            switch (mode) {
                case Mode.Ndof:
                    expected = new byte[][] {
                        new byte[] { 0x19, 0x02, 0x01, CONFIG_MASKS[(int)acc][(int)gyr] },
                        new byte[] { 0x03, 0x03, 0x8, BMI160_ACC_RANGE_BITMASK[(int)acc] },
                        configGyro100Hz,
                        new byte[] { 0x15, 0x04, 0x04, 0x0e},
                        new byte[] { 0x15, 0x03, 0x6}
                    };
                    break;
                case Mode.ImuPlus:
                    expected = new byte[][] {
                        new byte[] { 0x19, 0x02, 0x02, CONFIG_MASKS[(int)acc][(int)gyr] },
                        new byte[] { 0x03, 0x03, 0x8, BMI160_ACC_RANGE_BITMASK[(int)acc] },
                        configGyro100Hz
                    };
                    break;
                case Mode.Compass:
                    expected = new byte[][] {
                        new byte[] { 0x19, 0x02, 0x03, CONFIG_MASKS[(int)acc][(int)gyr] },
                        new byte[] { 0x03, 0x03, 0x6, BMI160_ACC_RANGE_BITMASK[(int)acc]},
                        new byte[] { 0x15, 0x04, 0x04, 0x0e},
                        new byte[] { 0x15, 0x03, 0x6}
                    };
                    break;
                case Mode.M4g:
                    expected = new byte[][] {
                        new byte[] { 0x19, 0x02, 0x04, CONFIG_MASKS[(int)acc][(int)gyr] },
                        new byte[] { 0x03, 0x03, 0x7, BMI160_ACC_RANGE_BITMASK[(int)acc] },
                        new byte[] { 0x15, 0x04, 0x04, 0x0e },
                        new byte[] { 0x15, 0x03, 0x6 }
                    };
                    break;
            }
            expected[1][2] |= (byte)((int)accFilter << 4);

            sensorFusion.Configure(mode, acc, gyr, accExtra: new Object[] { accFilter }, gyroExtra: new object[] { gyroFilter });

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Parallelizable]
        [TestCaseSource(typeof(SensorFusionBoschTestDataClass), "StartAndStopTestCases")]
        public void StartAndStop(String property, byte[][] expected) {
            Type type = typeof(ISensorFusionBosch);
            var producer = type.GetProperty(property).GetValue(sensorFusion) as IAsyncDataProducer;

            producer.Start();
            sensorFusion.Configure(mode: Mode.Ndof);
            sensorFusion.Start();
            sensorFusion.Stop();
            producer.Stop();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Parallelizable]
        [TestCaseSource(typeof(SensorFusionBoschTestDataClass), "InterpretDataTestCases")]
        public async Task InterpretDataAsync(String property, object expected, byte[] response) {
            Type type = typeof(ISensorFusionBosch), valueType = expected.GetType();
            var producer = type.GetProperty(property).GetValue(sensorFusion) as IAsyncDataProducer;

            object actual = null;
            await producer.AddRouteAsync(source => source.Stream(data => {
                if (valueType == typeof(Quaternion)) {
                    actual = data.Value<Quaternion>();
                } else if (valueType == typeof(EulerAngles)) {
                    actual = data.Value<EulerAngles>();
                } else if (valueType == typeof(Acceleration)) {
                    actual = data.Value<Acceleration>();
                } else if (valueType == typeof(CorrectedAcceleration)) {
                    actual = data.Value<CorrectedAcceleration>();
                } else if (valueType == typeof(CorrectedAngularVelocity)) {
                    actual = data.Value<CorrectedAngularVelocity>();
                } else if (valueType == typeof(CorrectedMagneticField)) {
                    actual = data.Value<CorrectedMagneticField>();
                }
            }));

            platform.sendMockResponse(response);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class SensorFusionBoschTest : SensorFusionBoschBaseTest {        
        public SensorFusionBoschTest() : base() {

        }
        
        [Test]
        public void ReadCalibration() {
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await sensorFusion.ReadCalibrationStateAsync();
            });
        }
    }

    [Parallelizable]
    [TestFixture]
    class SensorFusionBoschRev1Test : SensorFusionBoschBaseTest {
        public SensorFusionBoschRev1Test() : base() {
            platform.initResponse.moduleResponses[0x19][3] = 0x1;
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            platform.customResponses.Add(new byte[] { 0x19, 0x8b },
                    new byte[] { 0x19, 0x8b, 0x00, 0x01, 0x02 });
        }

        [Test]
        public async Task ReadCalibration() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x19, 0x8b }
            };
            ImuCalibrationState expectedState = new ImuCalibrationState(CalibrationAccuracy.Unreliable, CalibrationAccuracy.LowAccuracy, CalibrationAccuracy.MediumAccuracy);

            var actual = await sensorFusion.ReadCalibrationStateAsync();

            Assert.That(actual, Is.EqualTo(expectedState));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
