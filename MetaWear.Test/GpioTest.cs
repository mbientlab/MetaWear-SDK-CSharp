using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Gpio;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [TestFixture]
    class GpioTest : UnitTestBase {
        private IGpio gpio;

        public GpioTest() : base(typeof(IGpio)) {

        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            gpio = metawear.GetModule<IGpio>();
        }

        [Test]
        public async Task ReadAnalogAsync() {
            byte[][] expected = {
                new byte[] {0x05, 0x86, 0x02, 0xff, 0xff, 0x00, 0xff},
                new byte[] {0x05, 0x87, 0x03, 0xff, 0xff, 0x00, 0xff}
            };

            await gpio.Pins[2].AbsoluteReference.AddRouteAsync(source => source.Stream());
            gpio.Pins[2].AbsoluteReference.Read();
            await gpio.Pins[3].Adc.AddRouteAsync(source => source.Stream());
            gpio.Pins[3].Adc.Read();

            platform.fileSuffix = "gpio_analog";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SilentReadAnalog() {
            byte[][] expected = {
                new byte[] {0x05, 0xc6, 0x02, 0xff, 0xff, 0x00, 0xff},
                new byte[] {0x05, 0xc7, 0x03, 0xff, 0xff, 0x00, 0xff}
            };

            gpio.Pins[2].AbsoluteReference.Read();
            gpio.Pins[3].Adc.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleAnalogData() {
            float expectedAbsRef = 2.498f, actualAbsRef = 0;
            ushort expectedAdc = 882, actualAdc = 0;

            await gpio.Pins[1].AbsoluteReference.AddRouteAsync(source => source.Stream(data => actualAbsRef = data.Value<float>()));
            await gpio.Pins[1].Adc.AddRouteAsync(source => source.Stream(data => actualAdc = data.Value<ushort>()));

            platform.sendMockResponse(new byte[] { 0x05, 0x87, 0x01, 0x72, 0x03 });
            platform.sendMockResponse(new byte[] { 0x05, 0x86, 0x01, 0xc2, 0x09 });

            Assert.That(actualAbsRef, Is.EqualTo(expectedAbsRef).Within(0.001f));
            Assert.That(actualAdc, Is.EqualTo(expectedAdc));
        }

        [Test]
        public void SetDo() {
            byte[][] expected = { new byte[] { 0x05, 0x01, 0x00 } };

            gpio.Pins[0].SetOutput();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void ClearDo() {
            byte[][] expected = { new byte[] { 0x05, 0x02, 0x01 } };

            gpio.Pins[1].ClearOutput();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        static object[] PullModes = {
            new object[] { (byte) 2, (byte) 5, PullMode.None },
            new object[] { (byte) 3, (byte) 4, PullMode.Down },
            new object[] { (byte) 4, (byte) 3, PullMode.Up },
        };
        [TestCaseSource("PullModes")]
        public void SetPullMode(byte pin, byte param, PullMode mode) {
            byte[][] expected = { new byte[] {0x05, param, pin} };

            gpio.Pins[pin].SetPullMode(mode);

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadDigital() {
            byte[][] expected = { new byte[] { 0x05, 0x88, 0x04 } };

            await gpio.Pins[4].Digital.AddRouteAsync(source => source.Stream());
            gpio.Pins[4].Digital.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SilentReadDigital() {
            byte[][] expected = { new byte[] { 0x05, 0xc8, 0x04 } };

            gpio.Pins[4].Digital.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleDigitalData() {
            byte[] expected = new byte[] { 1, 0 }, actual = new byte[2];

            int i = 0;
            await gpio.Pins[7].Digital.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<byte>()));

            platform.sendMockResponse(new byte[] { 0x05, 0x88, 0x07, 0x01 });
            platform.sendMockResponse(new byte[] { 0x05, 0x88, 0x07, 0x00 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        static object[] ChangeTypes = {
            new object[] { (byte) 5, (byte) 3, PinChangeType.Any },
            new object[] { (byte) 6, (byte) 2, PinChangeType.Falling },
            new object[] { (byte) 7, (byte) 1, PinChangeType.Rising },
        };
        [TestCaseSource("ChangeTypes")]
        public void SetPinChangeType(byte pin, byte param, PinChangeType type) {
            byte[][] expected = { new byte[] {0x05, 0x09, pin, param} };

            gpio.Pins[pin].SetChangeType(type);

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
        
        [Test]
        public async Task HandleMonitorData() {
            byte[] expected = new byte[] { 1, 0 }, actual = new byte[2];

            int i = 0;
            await gpio.Pins[0].Monitor.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<byte>()));

            platform.sendMockResponse(new byte[] { 0x05, 0x0a, 0x00, 0x01 });
            platform.sendMockResponse(new byte[] { 0x05, 0x0a, 0x00, 0x00 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task SubscribeMonitor() {
            byte[][] expected = { new byte[] { 0x5, 0xa, 0x1 } };

            await gpio.Pins[0].Monitor.AddRouteAsync(source => source.Stream());

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void StartStopMonitor() {
            byte[][] expected = {
                new byte[] { 0x05, 0x0b, 0x06, 0x01 },
                new byte[] { 0x05, 0x0b, 0x06, 0x00 }
            };

            gpio.Pins[6].Monitor.Start();
            gpio.Pins[6].Monitor.Stop();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
