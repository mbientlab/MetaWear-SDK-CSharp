using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [TestFixture]
    class SwitchTest : UnitTestBase {
        private ISwitch switchModule;

        public SwitchTest() : base(typeof(ISwitch)) {
        }

        [SetUp]
        public override void SetUp() {
            base.SetUp();

            switchModule = metawear.GetModule<ISwitch>();
        }

        [Test]
        public void SubAndUnSub() {
            byte[][] expected = new byte[][] {
                new byte[] {0x1, 0x1, 0x1 },
                new byte[] {0x1, 0x1, 0x0 }
            };

            Task<IRoute> task = switchModule.State.AddRouteAsync(source => source.Stream(null));
            task.Wait();

            task.Result.Remove();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void HandleData() {
            byte[] expected = new byte[] { 0x1, 0x0 }, 
                actual = new byte[] { 0xff, 0xff };

            int i = 0;
            Task<IRoute> task = switchModule.State.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<byte>()));
            task.Wait();

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
        public void HandleStateData() {
            byte[] expected = new byte[] { 0x1, 0x0 },
                actual = new byte[] { 0xff, 0xff };

            Task<byte> task = switchModule.State.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x01, 0x81, 0x1 });
            task.Wait();
            actual[0] = task.Result;

            task = switchModule.State.ReadAsync();
            platform.sendMockResponse(new byte[] { 0x01, 0x81, 0x0 });
            task.Wait();
            actual[1] = task.Result;

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
