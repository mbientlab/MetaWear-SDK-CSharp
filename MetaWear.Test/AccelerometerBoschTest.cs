using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class AccelerometerBoschTestFixtureData {
        public static IEnumerable Params {
            get {
                yield return new TestFixtureData(typeof(IAccelerometerBmi160));
                yield return new TestFixtureData(typeof(IAccelerometerBma255));
            }
        }
    }

    class AccelerometerBoschTest {
        abstract class TestBase : UnitTestBase {
            protected IAccelerometerBosch accelerometer;

            protected TestBase(Type type) : base(type) {
            }

            [SetUp]
            public async override Task SetUp() {
                await base.SetUp();

                accelerometer = metawear.GetModule<IAccelerometerBosch>();
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestLowHigh : TestBase {
            public TestLowHigh(Type accType) : base(accType) {
            }

            [Test]
            public void Stop() {
                byte[] expected = new byte[] { 0x03, 0x06, 0x00, 0x0f };

                accelerometer.LowAndHighG.Stop();
                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void ConfigureLow() {
                byte[] expected = accelerometer is IAccelerometerBmi160 ?
                    new byte[] { 0x03, 0x07, 0x07, 0x40, 0x85, 0x0b, 0xc0 } :
                    new byte[] { 0x03, 0x07, 0x09, 0x40, 0x85, 0x0f, 0xc0 };

                accelerometer.Configure(range: 16f);
                accelerometer.LowAndHighG.Configure(enableLowG: true, lowThreshold: 0.5f, lowDuration: 20, mode: LowGMode.Sum);

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void StartLow() {
                byte[] expected = new byte[] { 0x03, 0x06, 0x08, 0x00 };

                accelerometer.LowAndHighG.Configure(enableLowG: true);
                accelerometer.LowAndHighG.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public async Task HandleLowResponse() {
                LowHighG actual = null, expected = new LowHighG(false, true, false, false, false, Data.Sign.Positive);

                await accelerometer.LowAndHighG.AddRouteAsync(source => source.Stream(data => actual = data.Value<LowHighG>()));
                platform.sendMockResponse(new byte[] { 0x03, 0x08, 0x02 });

                Assert.That(actual, Is.EqualTo(expected));

            }

            [Test]
            public void ConfigureHigh() {
                byte[] expected = accelerometer is IAccelerometerBmi160 ?
                    new byte[] { 0x03, 0x07, 0x07, 0x30, 0x81, 0x05, 0x20 } :
                    new byte[] { 0x03, 0x07, 0x09, 0x30, 0x81, 0x06, 0x20 };

                accelerometer.Configure(range: 16f);
                accelerometer.LowAndHighG.Configure(enableHighGz: true, highThreshold: 2f, highDuration: 15);

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void StartHigh() {
                byte[] expected = new byte[] { 0x03, 0x06, 0x04, 0x00 };

                accelerometer.LowAndHighG.Configure(enableHighGz: true);
                accelerometer.LowAndHighG.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            static object[] HighResponses = {
                new object[] { new LowHighG(true, false, false, true, false, Sign.Negative), new byte[] { 0x03, 0x08, 0x29 } },
                new object[] { new LowHighG(true, false, false, true, false, Sign.Positive), new byte[] { 0x03, 0x08, 0x09 } },
                new object[] { new LowHighG(true, false, false, false, true, Sign.Negative), new byte[] { 0x03, 0x08, 0x31 } },
                new object[] { new LowHighG(true, false, false, false, true, Sign.Positive), new byte[] { 0x03, 0x08, 0x11 } },
                new object[] { new LowHighG(true, false, true, false, false, Sign.Positive), new byte[] { 0x03, 0x08, 0x05 } },
                new object[] { new LowHighG(true, false, true, false, false, Sign.Negative), new byte[] { 0x03, 0x08, 0x25 } }
            };
            [TestCaseSource("HighResponses")]
            public async Task HandleHighResponse(LowHighG expected, byte[] response) {
                LowHighG actual = null;

                await accelerometer.LowAndHighG.AddRouteAsync(source => source.Stream(data => actual = data.Value<LowHighG>()));
                platform.sendMockResponse(response);

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestFlat : TestBase {
            public TestFlat(Type accType) : base(accType) {
            }

            [Test]
            public void Start() {
                byte[][] expected = {
                    new byte[] { 0x03, 0x12, 0x01, 0x00 }
                };

                accelerometer.Flat.Start();

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[][] expected = {
                    new byte[] { 0x03, 0x12, 0x00, 0x01 }
                };

                accelerometer.Flat.Stop();

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            static object[] FlatResponses = {
                new object[] { true, new byte[] {0x03, 0x14, 0x03} },
                new object[] { false, new byte[] {0x03, 0x14, 0x01} },
            };
            [TestCaseSource("FlatResponses")]
            public virtual async Task HandleResponse(bool expected, byte[] response) {
                bool? actual = null;

                await accelerometer.Flat.AddRouteAsync(source => source.Stream(data => actual = data.Value<bool>()));
                platform.sendMockResponse(response);

                Assert.That(actual.Value, Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestFlatRev2 : TestFlat {
            public TestFlatRev2(Type accType) : base(accType) {
                platform.initResponse.moduleResponses[0x3][3] = 0x2;
            }

            static object[] FlatResponses = {
                new object[] { true, new byte[] {0x03, 0x14, 0x07} },
                new object[] { false, new byte[] {0x03, 0x14, 0x03}},
            };
            [TestCaseSource("FlatResponses")]
            public override async Task HandleResponse(bool expected, byte[] response) {
                bool? actual = null;

                await accelerometer.Flat.AddRouteAsync(source => source.Stream(data => actual = data.Value<bool>()));
                platform.sendMockResponse(response);

                Assert.That(actual.Value, Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestNoMotion : TestBase {
            public TestNoMotion(Type accType) : base(accType) { }

            [Test]
            public void Start() {
                byte[] expected = accelerometer is IAccelerometerBmi160 ? new byte[] { 0x03, 0x09, 0x38, 0x00 } : new byte[] { 0x03, 0x09, 0x78, 0x00 };

                accelerometer.Motion.ConfigureNo();
                accelerometer.Motion.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[] expected = accelerometer is IAccelerometerBmi160 ? new byte[] { 0x03, 0x09, 0x00, 0x38 } : new byte[] { 0x03, 0x09, 0x00, 0x78 };

                accelerometer.Motion.ConfigureNo();
                accelerometer.Motion.Stop();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Configure() {
                byte[][] expected = {
                    accelerometer is IAccelerometerBmi160 ? new byte[] { 0x03, 0x0a, 0x18, 0x14, 0x7f, 0x15 } : new byte[] { 0x03, 0x0a, 0x24, 0x14, 0x7f }
                };

                accelerometer.Motion.ConfigureNo(duration: 10000, threshold: 0.5f);

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public void HandleData() {
                byte? actual = null;

                accelerometer.Motion.AddRouteAsync(source => source.Stream(data => actual = data.Bytes[0]));
                platform.sendMockResponse(new byte[] { 0x03, 0x0b, 0x04 });

                Assert.That(actual.Value, Is.EqualTo(4));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestSlowMotion : TestBase {
            public TestSlowMotion(Type accType) : base(accType) { }

            [Test]
            public void Start() {
                byte[] expected = new byte[] { 0x03, 0x09, 0x38, 0x00 };

                accelerometer.Motion.ConfigureSlow();
                accelerometer.Motion.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[] expected = new byte[] { 0x03, 0x09, 0x00, 0x38 };

                accelerometer.Motion.ConfigureSlow();
                accelerometer.Motion.Stop();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Configure() {
                byte[] expected = accelerometer is IAccelerometerBmi160 ? new byte[] { 0x03, 0x0a, 0x10, 0x14, 0xc0, 0x14 } : new byte[] { 0x03, 0x0a, 0x10, 0x14, 0xc0 };

                accelerometer.Configure(range: 4f);
                accelerometer.Motion.ConfigureSlow(threshold: 1.5f, count: 5);

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void HandleData() {
                byte? actual = null;

                accelerometer.Motion.AddRouteAsync(source => source.Stream(data => actual = data.Bytes[0]));
                platform.sendMockResponse(new byte[] { 0x03, 0x0b, 0x04 });

                Assert.That(actual.Value, Is.EqualTo(4));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestAnyMotion : TestBase {
            public TestAnyMotion(Type accType) : base(accType) { }

            [Test]
            public void Start() {
                byte[] expected = new byte[] { 0x03, 0x09, 0x07, 0x00 };

                accelerometer.Motion.ConfigureAny();
                accelerometer.Motion.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[] expected = new byte[] { 0x03, 0x09, 0x00, 0x7 };

                accelerometer.Motion.ConfigureAny();
                accelerometer.Motion.Stop();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Configure() {
                byte[] expected = accelerometer is IAccelerometerBmi160 ? new byte[] { 0x03, 0x0a, 0x09, 0x2f, 0x14, 0x14 } : new byte[] { 0x03, 0x0a, 0x09, 0x2f, 0x14 };

                accelerometer.Configure(range: 8f);
                accelerometer.Motion.ConfigureAny(threshold: 0.75f, count: 10);

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            static object[] AnyMotionResponses = {
                new object[] { new AnyMotion(Data.Sign.Positive, false, false, true), new byte[] {0x03, 0x0b, 0x22} },
                new object[] { new AnyMotion(Data.Sign.Negative, false, false, true), new byte[] {0x03, 0x0b, 0x62} },
                new object[] { new AnyMotion(Data.Sign.Negative, false, true, false), new byte[] {0x03, 0x0b, 0x52} },
                new object[] { new AnyMotion(Data.Sign.Positive, false, true, false), new byte[] {0x03, 0x0b, 0x12} },
                new object[] { new AnyMotion(Data.Sign.Positive, true, false, false), new byte[] {0x03, 0x0b, 0x0a} },
                new object[] { new AnyMotion(Data.Sign.Negative, true, false, false), new byte[] { 0x03, 0x0b, 0x4a } }
            };
            [TestCaseSource("AnyMotionResponses")]
            public async Task HandleData(AnyMotion expected, byte[] response) {
                AnyMotion actual = null;

                await accelerometer.Motion.AddRouteAsync(source => source.Stream(data => actual = data.Value<AnyMotion>()));
                platform.sendMockResponse(response);

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestOrientation : TestBase {
            public TestOrientation(Type accType) : base(accType) { }

            [Test]
            public void Start() {
                byte[][] expected = {
                    new byte[] { 0x03, 0x0f, 0x01, 0x00 }
                };

                accelerometer.Orientation.Start();
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[][] expected = {
                    new byte[] { 0x03, 0x0f, 0x00, 0x01 }
                };

                accelerometer.Orientation.Stop();
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            static object[] OrientationResponses = {
                new object[] { SensorOrientation.FaceUpLandscapeRight, new byte[] { 0x03, 0x11, 0x07 } },
                new object[] { SensorOrientation.FaceUpPortraitUpright, new byte[] { 0x03, 0x11, 0x01 } },
                new object[] { SensorOrientation.FaceUpPortraitUpsideDown, new byte[] { 0x03, 0x11, 0x03 } },
                new object[] { SensorOrientation.FaceUpLandscapeLeft, new byte[] { 0x03, 0x11, 0x05 } },
                new object[] { SensorOrientation.FaceDownLandscapeRight, new byte[] { 0x03, 0x11, 0x0f } },
                new object[] { SensorOrientation.FaceDownLandscapeLeft, new byte[] { 0x03, 0x11, 0x0d } },
                new object[] { SensorOrientation.FaceDownPortraitUpright, new byte[] { 0x03, 0x11, 0x09 } },
                new object[] { SensorOrientation.FaceDownPortraitUpsideDown, new byte[] { 0x03, 0x11, 0x0b } }
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
        [TestFixtureSource(typeof(AccelerometerBoschTestFixtureData), "Params")]
        class TestTap : TestBase {
            public TestTap(Type accType) : base(accType) { }

            [Test]
            public void ConfigureSingle() {
                byte[] expected = new byte[] { 0x03, 0x0d, 0x04, 0x04 };

                accelerometer.Configure(range: 16f);
                accelerometer.Tap.Configure(threshold: 2f, shock: TapShockTime._50ms);

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void StartSingle() {
                byte[] expected = new byte[] { 0x03, 0x0c, 0x02, 0x00 };

                accelerometer.Tap.Configure(enableSingle: true);
                accelerometer.Tap.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            static object[] SingleResponses = {
                new object[] { new Tap(TapType.Single, Sign.Positive), new byte[] { 0x03, 0x0e, 0x12 } },
                new object[] { new Tap(TapType.Single, Sign.Negative), new byte[] { 0x03, 0x0e, 0x32 } },                
            };
            [TestCaseSource("SingleResponses")]
            public async Task HandleSingleData(Tap expected, byte[] response) {
                Tap actual = null;

                await accelerometer.Tap.AddRouteAsync(source => source.Stream(data => actual = data.Value<Tap>()));
                platform.sendMockResponse(response);

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void ConfigureDouble() {
                byte[] expected = new byte[] { 0x03, 0x0d, 0xc4, 0x04 };

                accelerometer.Configure(range: 8f);
                accelerometer.Tap.Configure(threshold: 1f, window: DoubleTapWindow._50ms, quiet: TapQuietTime._20ms, shock: TapShockTime._75ms);

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void StartDouble() {
                byte[] expected = new byte[] { 0x03, 0x0c, 0x01, 0x00 };

                accelerometer.Tap.Configure(enableDouble: true);
                accelerometer.Tap.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            static object[] DoubleResponses = {
                new object[] { new Tap(TapType.Double, Sign.Positive), new byte[] { 0x03, 0x0e, 0x11 } },
                new object[] { new Tap(TapType.Double, Sign.Negative), new byte[] { 0x03, 0x0e, 0x31 } },
            };
            [TestCaseSource("DoubleResponses")]
            public async Task HandleDoubleData(Tap expected, byte[] response) {
                Tap actual = null;

                await accelerometer.Tap.AddRouteAsync(source => source.Stream(data => actual = data.Value<Tap>()));
                platform.sendMockResponse(response);

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[][] expected = {
                    new byte[] { 0x03, 0x0c, 0x00, 0x03 }
                };

                accelerometer.Tap.Stop();

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }
        }
    }
}
