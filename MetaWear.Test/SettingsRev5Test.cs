using MbientLab.MetaWear.Core;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class SettingsRev5Test : UnitTestBase {
        private ISettings settings;

        public SettingsRev5Test() : base(typeof(ISettings)) {
            platform.initResponse.moduleResponses[0x11] = new byte[] { 0x11, 0x80, 0x00, 0x05, 0x03 };
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            settings = metawear.GetModule<ISettings>();
        }

        [Test]
        public async Task Serialize() {
            await settings.Battery.AddRouteAsync(source => source.Stream());
            await settings.ChargeStatus.AddRouteAsync(source => source.Stream());
            await settings.PowerStatus.AddRouteAsync(source => source.Stream());

            platform.fileSuffix = "settings_rev5";
            await metawear.SerializeAsync();
        }

        [Test]
        public void ReadPowerStatus() {
            byte[][] expected = { new byte[] { 0x11, 0x91 } };

            settings.PowerStatus.ReadAsync();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task PowerStatusDataAsync() {
            byte[] expected = new byte[] { 0x1, 0x0 };
            byte[] actual = new byte[2];
            int i = 0;

            await settings.PowerStatus.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<byte>()));
            platform.sendMockResponse(new byte[] { 0x11, 0x11, 0x01 });
            platform.sendMockResponse(new byte[] { 0x11, 0x11, 0x00 });
            

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadPowerStatusDataAsync() {
            byte[] expected = new byte[] { 0x1, 0x0 };
            byte[] actual = new byte[2];

            var task = settings.PowerStatus.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x11, 0x91, 0x01 });
            actual[0] = await task;

            task = settings.PowerStatus.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x11, 0x91, 0x00 });
            actual[1] = await task;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ReadChargeStatus() {
            byte[][] expected = { new byte[] { 0x11, 0x92 } };

            settings.ChargeStatus.ReadAsync();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ChargeStatusDataAsync() {
            byte[] expected = new byte[] { 0x1, 0x0 };
            byte[] actual = new byte[2];
            int i = 0;

            await settings.ChargeStatus.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<byte>()));
            platform.sendMockResponse(new byte[] { 0x11, 0x12, 0x01 });
            platform.sendMockResponse(new byte[] { 0x11, 0x12, 0x00 });
            
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadChargeStatusDataAsync() {
            byte[] expected = new byte[] { 0x1, 0x0 };
            byte[] actual = new byte[2];

            var task = settings.ChargeStatus.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x11, 0x92, 0x01 });
            actual[0] = await task;

            task = settings.ChargeStatus.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x11, 0x92, 0x00 });
            actual[1] = await task;

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
