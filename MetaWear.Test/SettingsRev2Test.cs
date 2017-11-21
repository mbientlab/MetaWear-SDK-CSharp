using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [TestFixture]
    class SettingsRev2Test : UnitTestBase {
        private ISettings settings;

        public SettingsRev2Test() : base(typeof(ISettings), typeof(ILed)) {
            platform.initResponse.moduleResponses[0x11] = new byte[] { 0x11, 0x80, 0x00, 0x02 };
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            settings = metawear.GetModule<ISettings>();
        }

        [Test]
        public async Task DisconnectEventAsync() {
            byte[][] expected = {
                new byte[] {0x0a, 0x02, 0x11, 0x0a, 0xff, 0x02, 0x03, 0x0f},
                new byte[] {0x0a, 0x03, 0x02, 0x02, 0x1f, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0xf4, 0x01, 0x00, 0x00, 0x0a},
                new byte[] {0x0a, 0x02, 0x11, 0x0a, 0xff, 0x02, 0x01, 0x01},
                new byte[] {0x0a, 0x03, 0x01}
            };
            var led = metawear.GetModule<ILed>();

            await settings.OnDisconnectAsync(() => {
                led.EditPattern(Color.Blue, highTime: 50, duration: 500, high: 31, count: 10);
                led.Play();
            });
            platform.fileSuffix = "observer_rev2";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void NullBattery() {
            Assert.That(settings.Battery, Is.Null);
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
