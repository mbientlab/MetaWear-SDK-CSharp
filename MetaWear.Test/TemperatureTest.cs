using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.Temperature;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class TemperatureTest : UnitTestBase {
        static object[] Channels = {
            new object[] { 0 },
            new object[] { 1 },
            new object[] { 2 },
            new object[] { 3 }
        }, Responses = {
            new object[] {0, new byte[] { 0x04, 0x81, 0x00, 0xfe, 0x00 }, 31.75f },
            new object[] {1, new byte[] { 0x04, 0x81, 0x01, 0xa8, 0x00 }, 21f },
            new object[] {2, new byte[] { 0x04, 0x81, 0x02, 0xac, 0xff }, -10.5f },
            new object[] {3, new byte[] { 0x04, 0x81, 0x03, 0x00, 0x00 }, 0f }
        };

        private ITemperature temperature;

        public TemperatureTest() : base(typeof(ITemperature)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            temperature = metawear.GetModule<ITemperature>();
        }

        [Test]
        public void ConfigureExtThermistor() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x04, 0x02, 0x02, 0x00, 0x01, 0x00 }
            };

            var sensor = temperature.FindSensors(SensorType.ExtThermistor)[0] as IExternalThermistor;
            sensor.Configure(0, 1, false);

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void CheckSourceCount() {
            Assert.That(temperature.Sensors.Count, Is.EqualTo(4));
        }

        [TestCaseSource("Channels")]
        public async Task ReadAsync(int channel) {
            byte[][] expected = new byte[][] {
                new byte[] { 0x4, 0x81, (byte) channel }
            };

            var sensor = temperature.Sensors[channel];
            await sensor.AddRouteAsync(source => source.Stream());
            temperature.Sensors[channel].Read();

            platform.fileSuffix = "temperature";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [TestCaseSource("Channels")]
        public void SilentRead(int channel) {
            byte[][] expected = new byte[][] {
                new byte[] { 0x4, 0xc1, (byte) channel }
            };

            var sensor = temperature.Sensors[channel];
            temperature.Sensors[channel].Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [TestCaseSource("Responses")]
        public async Task HandleDataAsync(int channel, byte[] response, float expected) {
            float actual = 0;

            var sensor = temperature.Sensors[channel];
            await sensor.AddRouteAsync(source => source.Stream(data => actual = data.Value<float>()));
            platform.sendMockResponse(response);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
