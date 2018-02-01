using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Core.Settings;
using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class SettingsRev3Test : UnitTestBase {
        private ISettings settings;

        public SettingsRev3Test() : base(typeof(ISettings), typeof(IHaptic)) {
            platform.initResponse.moduleResponses[0x11] = new byte[] { 0x11, 0x80, 0x00, 0x03 };
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            settings = metawear.GetModule<ISettings>();
        }

        [Test]
        public async Task DisconnectEventAsync() {
            byte[][] expected = {
                new byte[] {0x0a, 0x02, 0x11, 0x0a, 0xff, 0x08, 0x01, 0x04},
                new byte[] {0x0a, 0x03, 0xf8, 0xb8, 0x0b, 0x00}
            };
            var haptic = metawear.GetModule<IHaptic>();

            await settings.OnDisconnectAsync(() => haptic.StartMotor(3000));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void ReadBattery() {
            byte[][] expected = { new byte[] { 0x11, 0xcc } };

            settings.Battery.Read();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretBatteryDataAsync() {
            var expected = new BatteryState(99, BitConverter.ToSingle(new byte[] { 0x6a, 0xbc, 0x84, 0x40 }, 0));
            BatteryState actual = null;

            await settings.Battery.AddRouteAsync(source => source.Stream(data => actual = data.Value<BatteryState>()));
            platform.sendMockResponse(new byte[] { 0x11, 0x8c, 0x63, 0x34, 0x10 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretComponentBatteryDataAsync() {
            var expectedVoltage = BitConverter.ToSingle(new byte[] { 0x6a, 0xbc, 0x84, 0x40 }, 0);
            var expectedCharge = (byte) 99;

            byte actualCharge = 0;
            float actualVoltage = 0;

            await settings.Battery.AddRouteAsync(source => 
                source.Split().Index(0).Stream(data => actualCharge = data.Value<byte>())
                        .Index(1).Stream(data => actualVoltage = data.Value<float>())
            );
            platform.sendMockResponse(new byte[] { 0x11, 0x8c, 0x63, 0x34, 0x10 });

            Assert.That(actualCharge, Is.EqualTo(expectedCharge));
            Assert.That(actualVoltage, Is.EqualTo(expectedVoltage).Within(0.001f));
        }

        [Test]
        public void NullPowerStatus() {
            Assert.That(settings.PowerStatus, Is.Null);
        }

        [Test]
        public void NullChargeStatus() {
            Assert.That(settings.ChargeStatus, Is.Null);
        }
    }
}
