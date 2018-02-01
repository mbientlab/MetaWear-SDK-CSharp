using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Core.Settings;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
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
        public async Task ReadAdConfigAsync() {
            platform.customResponses.Add(new byte[] { 0x11, 0x81 },
                    new byte[] { 0x11, 0x81, 0x4d, 0x65, 0x74, 0x61, 0x57, 0x65, 0x61, 0x72 });
            platform.customResponses.Add(new byte[] { 0x11, 0x82 },
                    new byte[] { 0x11, 0x82, 0x9c, 0x02, 0x00, 0x00 });
            platform.customResponses.Add(new byte[] { 0x11, 0x87 },
                    new byte[] { 0x11, 0x87, 0x19, 0xff, 0x6d, 0x62, 0x74, 0x68, 0x65, 0x20, 0x53, 0x63, 0x61, 0x72, 0x6c, 0x65, 0x74, 0x74, 0x20, 0x73 });

            var expected = new BleAdvertisementConfig("MetaWear", 417, 0,
                new byte[] { 0x19, 0xff, 0x6d, 0x62, 0x74, 0x68, 0x65, 0x20, 0x53, 0x63, 0x61, 0x72, 0x6c, 0x65, 0x74, 0x74, 0x20, 0x73 });

            Assert.That(await settings.ReadBleAdConfigAsync(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadConnParamsAsync() {
            platform.customResponses.Add(new byte[] { 0x11, 0x89 },
                    new byte[] { 0x11, 0x89, 0x06, 0x00, 0x09, 0x00, 0x00, 0x00, 0x58, 0x02 });

            var expected = new BleConnectionParameters(7.5f, 11.25f, 0, 6000);

            Assert.That(await settings.ReadBleConnParamsAsync(), Is.EqualTo(expected));
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
