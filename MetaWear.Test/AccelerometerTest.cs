using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class AccelerometerTestFixtureData {
        public static IEnumerable Params {
            get {
                yield return new TestFixtureData(typeof(IAccelerometerMma8452q));
                yield return new TestFixtureData(typeof(IAccelerometerBmi160));
                yield return new TestFixtureData(typeof(IAccelerometerBma255));
            }
        }
    }

    [Parallelizable]
    [TestFixtureSource(typeof(AccelerometerTestFixtureData), "Params")]
    class AccelerometerTest : UnitTestBase {
        private IAccelerometer accelerometer;

        public AccelerometerTest(Type accType) : base(typeof(ILogging), typeof(IDataProcessor), accType) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            accelerometer = metawear.GetModule<IAccelerometer>();
        }

        [Test]
        public async Task CreateAndRemoveStreamAsync() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x03, 0x04, 0x01 },
                new byte[] { 0x03, 0x02, 0x01 },
                new byte[] { 0x03, 0x01, 0x01 },
                new byte[] { 0x03, 0x01, 0x00 },
                new byte[] { 0x03, 0x02, 0x00 },
                new byte[] { 0x03, 0x04, 0x00 }
            };

            if (accelerometer is IAccelerometerBosch) {
                expected[1] = new byte[] { 0x03, 0x02, 0x01, 0x00 };
                expected[4] = new byte[] { 0x03, 0x02, 0x00, 0x01 };
            }

            var route = await accelerometer.Acceleration.AddRouteAsync(source => source.Stream(null));
            accelerometer.Acceleration.Start();
            accelerometer.Start();

            accelerometer.Stop();
            accelerometer.Acceleration.Stop();
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleDataAsync() {
            FloatVector expected = null, actual = null;
            await accelerometer.Acceleration.AddRouteAsync(source => source.Stream(data => actual = data.Value<FloatVector>()).Name("acc_stream"));

            if (accelerometer is IAccelerometerMma8452q) {
                // (-1.450f, -2.555f, 0.792f)

                expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x9a, 0x99, 0xb9, 0xbf }, 0),
                    BitConverter.ToSingle(new byte[] { 0x1f, 0x85, 0x23, 0xc0 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x83, 0xc0, 0x4a, 0x3f }, 0));

                platform.sendMockResponse(new byte[] { 0x03, 0x04, 0x56, 0xfa, 0x05, 0xf6, 0x18, 0x03 });
            } else if (accelerometer is IAccelerometerBmi160) {
                // (-1.872f, -2.919f, -1.495f)

                expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x00, 0xa8, 0xef, 0xbf }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0xd8, 0x3a, 0xc0 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0x58, 0xbf, 0xbf }, 0));

                accelerometer.Configure(range: 4f);
                platform.sendMockResponse(new byte[] { 0x03, 0x04, 0x16, 0xc4, 0x94, 0xa2, 0x2a, 0xd0 });

                platform.fileSuffix = "bmi160_acc_route";
                await metawear.SerializeAsync();

            } else if (accelerometer is IAccelerometerBma255) {
                // (-4.7576f, 2.2893f, 2.9182f)

                expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x00, 0x3e, 0x98, 0xc0 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0x84, 0x12, 0x40 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0xc4, 0x3a, 0x40 }, 0));

                accelerometer.Configure(range: 8f);
                platform.sendMockResponse(new byte[] { 0x03, 0x04, 0xe1, 0xb3, 0xa1, 0x24, 0xb1, 0x2e });
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task CreateAndRemoveLogAsync() {
            byte[][] expected = new byte[][]{
                new byte[] {0x0b, 0x02, 0x03, 0x04, 0xff, 0x60},
                new byte[] {0x0b, 0x02, 0x03, 0x04, 0xff, 0x24},
                new byte[] {0x0b, 0x03, 0x00},
                new byte[] {0x0b, 0x03, 0x01}
            };

            var route = await accelerometer.Acceleration.AddRouteAsync(source => source.Log());
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleLogData() {
            Acceleration expected = null, actual = null;
            await accelerometer.Acceleration.AddRouteAsync(source => source.Log(data => actual = data.Value<Acceleration>()));

            if (accelerometer is IAccelerometerMma8452q) {
                // (-1.450f, -2.555f, 0.792f)

                expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x9a, 0x99, 0xb9, 0xbf }, 0),
                    BitConverter.ToSingle(new byte[] { 0x1f, 0x85, 0x23, 0xc0 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x83, 0xc0, 0x4a, 0x3f }, 0));

                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0xa0, 0xe6, 66, 0, 0, 0x56, 0xfa, 0x05, 0xf6, 0xa1, 0xe6, 66, 0, 0, 0x18, 0x03, 0, 0 });
            } else if (accelerometer is IAccelerometerBmi160) {
                // (-1.872f, -2.919f, -1.495f)

                expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x00, 0xa8, 0xef, 0xbf }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0xd8, 0x3a, 0xc0 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0x58, 0xbf, 0xbf }, 0));

                accelerometer.Configure(range: 4f);
                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0xa0, 0xe6, 66, 0, 0, 0x16, 0xc4, 0x94, 0xa2, 0xa1, 0xe6, 66, 0, 0, 0x2a, 0xd0, 0, 0 });
            } else if (accelerometer is IAccelerometerBma255) {
                // (-4.7576f, 2.2893f, 2.9182f)

                expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x00, 0x3e, 0x98, 0xc0 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0x84, 0x12, 0x40 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0xc4, 0x3a, 0x40 }, 0));

                accelerometer.Configure(range: 8f);
                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0xa0, 0xe6, 66, 0, 0, 0xe1, 0xb3, 0xa1, 0x24, 0xa1, 0xe6, 66, 0, 0, 0xb1, 0x2e, 0, 0 });
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleSingleAxisDataAsync() {
            float[] expected = null, actual = new float[3];

            await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Split().Index(0).Stream(data => actual[0] = data.Value<float>())
                        .Index(1).Stream(data => actual[1] = data.Value<float>())
                        .Index(2).Stream(data => actual[2] = data.Value<float>())
            );

            if (accelerometer is IAccelerometerMma8452q) {
                expected = new float[] { -1.450f, -2.555f, 0.792f };
                platform.sendMockResponse(new byte[] { 0x03, 0x04, 0x56, 0xfa, 0x05, 0xf6, 0x18, 0x03 });
            } else if (accelerometer is IAccelerometerBmi160) {
                expected = new float[] { -1.872f, -2.919f, -1.495f };
                accelerometer.Configure(range: 4f);
                platform.sendMockResponse(new byte[] { 0x03, 0x04, 0x16, 0xc4, 0x94, 0xa2, 0x2a, 0xd0 });
            } else if (accelerometer is IAccelerometerBma255) {
                expected = new float[] { -4.7576f, 2.2893f, 2.9182f };
                accelerometer.Configure(range: 8f);
                platform.sendMockResponse(new byte[] { 0x03, 0x04, 0xe1, 0xb3, 0xa1, 0x24, 0xb1, 0x2e });
            }

            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public async Task HandlePackedResponseAsync() {
            Acceleration[] expected = null, actual = new Acceleration[3];
            byte[] response = null;

            int i = 0;
            var route = await accelerometer.PackedAcceleration.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<Acceleration>()));

            if (accelerometer is IAccelerometerMma8452q) {
                expected = new Acceleration[] {
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x12, 0x83, 0x94, 0xc0 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x9a, 0x99, 0xb5, 0x40 }, 0),
                        BitConverter.ToSingle(new byte[] { 0xd7, 0xa3, 0x70, 0xbe }, 0)
                    ),
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0xa6, 0x9b, 0xb4, 0xbf }, 0),
                        BitConverter.ToSingle(new byte[] { 0x73, 0x68, 0xa1, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0xb0, 0x72, 0x30, 0x40 }, 0)
                    ),
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x68, 0x91, 0x9d, 0xbf }, 0),
                        BitConverter.ToSingle(new byte[] { 0xac, 0x1c, 0xea, 0xbf }, 0),
                        BitConverter.ToSingle(new byte[] { 0x06, 0x81, 0x5d, 0xc0 }, 0)
                    )
                };
                response = new byte[] {0x03, 0x12, 0xdf, 0xed, 0x2b, 0x16, 0x15, 0xff, 0x7d, 0xfa,
                    0xed, 0x04, 0xc5, 0x0a, 0x31, 0xfb, 0xdb, 0xf8, 0x7b, 0xf2};
            } else if (accelerometer is IAccelerometerBmi160) {
                expected = new Acceleration[] {
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x00, 0x3c, 0x91, 0xc0 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0x30, 0x55, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0xc0, 0x05, 0xbe }, 0)
                    ),
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x00, 0xa8, 0x3f, 0xc0 }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0xd0, 0x64, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0xc0, 0x15, 0x3e }, 0)
                    ),
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0xec, 0xbc }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0x20, 0xb4, 0x3e }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x85, 0x3d }, 0)
                    )
                };
                response = new byte[] {0x03, 0x1c, 0x62, 0xb7, 0x53, 0x0d, 0xe9, 0xfd, 0x16, 0xd0, 0x4d,
                    0x0e, 0x57, 0x02, 0x8a, 0xff, 0xa1, 0x05, 0x0a, 0x01};

                accelerometer.Configure(range: 8f);
            } else if (accelerometer is IAccelerometerBma255) {
                expected = new Acceleration[] {
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x00, 0xc4, 0x98, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0x60, 0x55, 0xbe }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0xca, 0x6e, 0x40 }, 0)
                    ),
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] { 0x00, 0xe4, 0xa4, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0xdc, 0x91, 0xbf }, 0),
                        BitConverter.ToSingle(new byte[] { 0x00, 0xfa, 0x7f, 0x40 }, 0)
                    ),
                    new Acceleration(
                        BitConverter.ToSingle(new byte[] {0x00, 0x54, 0xf6, 0x3f }, 0),
                        BitConverter.ToSingle(new byte[] {0x00, 0x7c, 0xfa, 0xbf }, 0),
                        BitConverter.ToSingle(new byte[] {0x00, 0xe2, 0x7f, 0x40 }, 0)
                    )
                };
                response = new byte[] {0x03, 0x1c, 0x31, 0x26, 0x55, 0xf9, 0x65, 0x77, 0x39, 0x29, 0x89, 0xdb,
                    0xfd, 0x7f, 0x95, 0x3d, 0x61, 0xc1, 0xf1, 0x7f};

                accelerometer.Configure(range: 4f);
            }

            platform.sendMockResponse(response);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(route.Subscribers[0].Identifier, Is.EqualTo("acceleration"));
        }
        
        [Test]
        public async Task YXFeedbackAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0x22, 0x0a, 0x01, 0x01},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x00, 0x20, 0x09, 0x15, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x02, 0x09, 0x03, 0x01, 0x20},
                new byte[] {0x0a, 0x02, 0x03, 0x04, 0xff, 0x09, 0x05, 0x09, 0x05, 0x04},
                new byte[] {0x0a, 0x03, 0x01, 0x09, 0x15, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x03, 0x01},
                new byte[] {0x09, 0x07, 0x01, 0x01},
                new byte[] {0x09, 0x07, 0x01, 0x00},
                new byte[] {0x0b, 0x03, 0x00},
                new byte[] {0x09, 0x06, 0x00},
                new byte[] {0x09, 0x06, 0x01},
                new byte[] {0x0a, 0x04, 0x00}
            };

            var route = await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Split().Index(0).Name("x-axis")
                        .Index(1).Delay(1).Map(Function2.Subtract, "x-axis").Stream().Log()
            );
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task XYAbsAddAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0x20, 0x09, 0x15, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0x22, 0x09, 0x15, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x20, 0x09, 0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x00, 0x09, 0x05, 0x09, 0x05, 0x04},
                new byte[] {0x0a, 0x03, 0x02, 0x09, 0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00}
            };

            await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Split().Index(0).Map(Function1.AbsValue).Name("x-abs")
                        .Index(1).Map(Function1.AbsValue).Map(Function2.Add, "x-abs")
            );
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void GenericConfigure() {
            float expectedOdr = 0f, expectedRange = 0f;

            if (accelerometer is IAccelerometerMma8452q) {
                accelerometer.Configure(odr: 12.4f, range: 10f);
                expectedOdr = 12.5f;
                expectedRange = 8f;
            } else if (accelerometer is IAccelerometerBmi160) {
                accelerometer.Configure(odr: 1.562f, range: 1f);
                expectedOdr = 1.5625f;
                expectedRange = 2f;
            } else if (accelerometer is IAccelerometerBma255) {
                accelerometer.Configure(odr: 3000f, range: 5f);
                expectedOdr = 2000f;
                expectedRange = 4f;
            }

            Assert.That(expectedOdr, Is.EqualTo(accelerometer.Odr).Within(0.001f));
            Assert.That(expectedRange, Is.EqualTo(accelerometer.Range).Within(0.001f));
        }
    }
}
