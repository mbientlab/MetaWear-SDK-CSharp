using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class SwitchTest : UnitTestBase {
        private ISwitch switchModule;

        public SwitchTest() : base(typeof(ISwitch)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            switchModule = metawear.GetModule<ISwitch>();
        }

        [Test]
        public async Task SubAndUnSub() {
            byte[][] expected = new byte[][] {
                new byte[] {0x1, 0x1, 0x1 },
                new byte[] {0x1, 0x1, 0x0 }
            };

            var route = await switchModule.State.AddRouteAsync(source => source.Stream(null));

            route.Remove();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleData() {
            byte[] expected = new byte[] { 0x1, 0x0 }, 
                actual = new byte[] { 0xff, 0xff };

            int i = 0;
            var route = await switchModule.State.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<byte>()));

            platform.sendMockResponse(new byte[] { 0x1, 0x1, 0x1 });
            platform.sendMockResponse(new byte[] { 0x1, 0x1, 0x0 });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ReadState() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x01, 0x81 }
            };

            Task<byte> task = switchModule.State.ReadAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleStateData() {
            byte[] expected = new byte[] { 0x1, 0x0 },
                actual = new byte[] { 0xff, 0xff };

            Task<byte> task = switchModule.State.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x01, 0x81, 0x1 });
            actual[0] = await task;

            task = switchModule.State.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x01, 0x81, 0x0 });
            actual[1] = await task;

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
