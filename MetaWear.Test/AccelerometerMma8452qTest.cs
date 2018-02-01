using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.AccelerometerMma8452q;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    class AccelerometerMma8452qTest {
        internal class AccelerometerMma8542qTestFixtureData {
            public static IEnumerable OutputDataRates {
                get {
                    foreach (var odr in Enum.GetValues(typeof(OutputDataRate))) {
                        yield return new TestFixtureData(odr);
                    }
                }
            }
        }

        static Dictionary<OutputDataRate, byte> ODR_MASKS = new Dictionary<OutputDataRate, byte>() {
            { OutputDataRate._800Hz, 0 },
            { OutputDataRate._400Hz, 0x8 },
            { OutputDataRate._200Hz, 0x10 },
            { OutputDataRate._100Hz, 0x18 },
            { OutputDataRate._50Hz, 0x20 },
            { OutputDataRate._12_5Hz, 0x28 },
            { OutputDataRate._6_25Hz, 0x30 },
            { OutputDataRate._1_56Hz, 0x38 }
        };
        class TestBase : UnitTestBase {
            protected IAccelerometerMma8452q accelerometer;

            public TestBase() : base(typeof(IAccelerometerMma8452q)) {
            }

            [SetUp]
            public async override Task SetUp() {
                await base.SetUp();

                accelerometer = metawear.GetModule<IAccelerometerMma8452q>();
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestOrientation : TestBase {
            public TestOrientation() : base() { }

            [Test]
            public void Start() {
                byte[][] expected = {
                        new byte[] {0x03, 0x03, 0x00, 0x00, 0x18, 0x02, 0x00},
                        new byte[] {0x03, 0x09, 0x00, 0xc0, 0x28, 0x44, 0x84},
                        new byte[] {0x03, 0x08, 0x01}
                };

                accelerometer.Configure(oversample: Oversampling.HighRes);
                accelerometer.Orientation.Start();
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[][] expected = {
                        new byte[] {0x03, 0x08, 0x00},
                        new byte[] {0x03, 0x09, 0x00, 0x80, 0x00, 0x44, 0x84}
                };

                accelerometer.Orientation.Stop();
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            static object[] OrientationResponses = {
                new object[] { SensorOrientation.FaceUpLandscapeRight, new byte[] {0x03, 0x0a, 0x84} },
                new object[] { SensorOrientation.FaceUpPortraitUpright, new byte[] { 0x03, 0x0a, 0x80 } },
                new object[] { SensorOrientation.FaceUpPortraitUpsideDown, new byte[] { 0x03, 0x0a, 0x82 } },
                new object[] { SensorOrientation.FaceUpLandscapeLeft, new byte[] { 0x03, 0x0a, 0x86 } },
                new object[] { SensorOrientation.FaceDownLandscapeRight, new byte[] { 0x03, 0x0a, 0x85 } },
                new object[] { SensorOrientation.FaceDownLandscapeLeft, new byte[] { 0x03, 0x0a, 0x87 } },
                new object[] { SensorOrientation.FaceDownPortraitUpright, new byte[] { 0x03, 0x0a, 0x81 } },
                new object[] { SensorOrientation.FaceDownPortraitUpsideDown, new byte[] { 0x03, 0x0a, 0x83 } }
            };
            [TestCaseSource("OrientationResponses")]
            public async Task HandleData(SensorOrientation expected, byte[] response) {
                SensorOrientation? actual = null;

                await accelerometer.Orientation.AddRouteAsync(source => source.Stream(data => actual = data.Value<SensorOrientation>()));
                platform.sendMockResponse(response);

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerMma8542qTestFixtureData), "OutputDataRates")]
        class TestHighPassFilter : TestBase {
            private OutputDataRate odr;

            public TestHighPassFilter(OutputDataRate odr) : base() {
                this.odr = odr;
            }

            static object[] NormalCutoffs = {
                new object[] { new float[] { 18f, 15.9f, 12f, 3.125f, 1.625f, 2.125f, 2f, 1.5625f }, (byte) 0b0 },
                new object[] { new float[] { 10f, 7.9f, 5.75f, 2.125f, 1.375f, 1.25f, 0.9f, 0.75f }, (byte) 0b1 },
                new object[] { new float[] { 5f, 3.9f, 1.75f, 1.125f, 0.375f, 0.6f, 0.74f, 0.4f }, (byte) 0b10 },
                new object[] { new float[] { 2.75f, 1.9f, 0.75f, 0.125f, 0.374f, 0.3f, 0.20f, 0.1f }, (byte) 0b11 }
            };
            [TestCaseSource("NormalCutoffs")]
            public void ConfigureNormal(float[] cutoffs, byte mask) {
                byte[][] expected = {
                    new byte[] { 0x03, 0x03, 0x10, mask, ODR_MASKS[odr], 0x00, 0x00 }
                };

                accelerometer.Configure(odr: odr, highPassCutoff: cutoffs[(int) odr]);
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            static object[] HighResCutoffs = {
                new object[] { new float[] { 18f, 15.9f, 12.01f, 16f, 31.41f, 13.125f, 17, 14 }, (byte) 0b0 },
                new object[] { new float[] { 10f, 7.9f, 6.75f, 11.99f, 8f, 6.01f, 7f, 6.25f }, (byte) 0b1 },
                new object[] { new float[] { 5f, 3.9f, 3.75f, 5.125f, 5.375f, 3.01f, 4.74f, 4f }, (byte) 0b10 },
                new object[] { new float[] { 2.75f, 1.9f, 0.75f, 0.125f, 0.374f, 0.3f, 0.20f, 0.1f }, (byte) 0b11 }
            };
            [TestCaseSource("HighResCutoffs")]
            public void ConfigureHighRes(float[] cutoffs, byte mask) {
                byte[][] expected = {
                    new byte[] { 0x03, 0x03, 0x10, mask, ODR_MASKS[odr], 0x02, 0x00 }
                };

                accelerometer.Configure(odr: odr, highPassCutoff: cutoffs[(int)odr], oversample: Oversampling.HighRes);
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }
        }
    }
}
