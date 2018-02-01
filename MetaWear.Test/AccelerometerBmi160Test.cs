using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.AccelerometerBmi160;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    class AccelerometerBmi160Test {
        class TestBase : UnitTestBase {
            protected IAccelerometerBmi160 accelerometer;

            public TestBase() : base(typeof(IAccelerometerBmi160)) {
            }

            [SetUp]
            public async override Task SetUp() {
                await base.SetUp();

                accelerometer = metawear.GetModule<IAccelerometerBmi160>();
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestStepCounter : TestBase {
            [Test]
            public async Task HandleCounterDataAsync() {
                ushort actual = 0;

                await accelerometer.StepCounter.AddRouteAsync(
                    source => source.Stream(data => actual = data.Value<ushort>())
                );
                platform.sendMockResponse(new byte[] { 0x03, 0x9a, 0x2b, 0x00 });

                Assert.That(actual, Is.EqualTo(43));
            }

            [Test]
            public async Task ReadCounter() {
                byte[][] expected = {
                    new byte[] { 0x03, 0x9a }
                };

                await accelerometer.StepCounter.AddRouteAsync(
                    source => source.Stream()
                );
                accelerometer.StepCounter.Read();

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public void SilentReadCounter() {
                byte[][] expected = {
                    new byte[] { 0x03, 0xda }
                };

                accelerometer.StepCounter.Read();

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestStepDetector : TestBase {
            [Test]
            public async Task StartAndStopStreamAsync() {
                byte[][] expected = new byte[][] {
                    new byte[] { 0x3, 0x19, 0x1 },
                    new byte[] { 0x03, 0x17, 0x01, 0x00 },
                    new byte[] { 0x03, 0x17, 0x00, 0x01 },
                    new byte[] { 0x3, 0x19, 0x0 }
                };
                var detector = accelerometer.StepDetector;

                var route = await detector.AddRouteAsync(source => source.Stream());
                detector.Start();

                detector.Stop();
                route.Remove();

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public async Task HandleDetectorDataAsync() {
                byte actual = 0xff;

                var route = await accelerometer.StepDetector.AddRouteAsync(source => source.Stream(data => actual = data.Value<byte>()));
                platform.sendMockResponse(new byte[] { 0x03, 0x19, 0x01 });

                Assert.That(actual, Is.EqualTo(0x1));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestSignificantMotion : TestBase {
            [Test]
            public void Start() {
                byte[] expected = new byte[] { 0x03, 0x09, 0x07, 0x00 };

                accelerometer.Motion.ConfigureSignificant();
                accelerometer.Motion.Start();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Stop() {
                byte[] expected = new byte[] { 0x03, 0x09, 0x00, 0x7 };

                accelerometer.Motion.ConfigureSignificant();
                accelerometer.Motion.Stop();

                Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
            }

            [Test]
            public void Configure() {
                byte[][] expected = {
                    new byte[] {0x03, 0x0a, 0x00, 0x14, 0x14, 0x36}
                };

                accelerometer.Motion.ConfigureSignificant(proof: ProofTime._1s, skip: SkipTime._1_5s);

                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            [Test]
            public async Task HandleData() {
                byte? actual = null;

                await accelerometer.Motion.AddRouteAsync(source => source.Stream(data => actual = data.Bytes[0]));
                platform.sendMockResponse(new byte[] { 0x03, 0x0b, 0x01 });

                Assert.That(actual.Value, Is.EqualTo(0x1));
            }
        }
    }
}
