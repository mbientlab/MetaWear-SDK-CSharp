using MbientLab.MetaWear.Sensor;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    class AccelerometerBmi160Test {
        class TestBase : UnitTestBase {
            protected IAccelerometerBmi160 accelerometer;

            public TestBase() : base(typeof(IAccelerometerBmi160)) {
            }

            [SetUp]
            public override void SetUp() {
                base.SetUp();

                accelerometer = metawear.GetModule<IAccelerometerBmi160>();
            }
        }

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
    }
}
