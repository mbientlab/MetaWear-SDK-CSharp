using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class DebugTest : UnitTestBase {
        private IDebug debug;

        public DebugTest() : base(typeof(IDebug), typeof(ISwitch)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            debug = metawear.GetModule<IDebug>();
        }

        [Test]
        public async Task ResetAsync() {
            byte[][] expected = { new byte[] { 0xfe, 0x01 } };

            await debug.ResetAsync();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            Assert.That(platform.nDisconnects, Is.EqualTo(1));
        }

        [Test]
        public async Task ResetInRoute() {
            byte[][] expected = {
                new byte[] { 0xa, 0x2, 0x1, 0x1, 0xff, 0xfe, 0x1, 0x00 },
                new byte[] { 0xa, 0x3 },
            };

            await metawear.GetModule<ISwitch>().State.AddRouteAsync(source => source.React(token => debug.ResetAsync()));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            Assert.That(platform.nDisconnects, Is.EqualTo(0));
        }

        [Test]
        public async Task JumpToBootloaderAsync() {
            byte[][] expected = { new byte[] { 0xfe, 0x02 } };

            await debug.JumpToBootloaderAsync();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            Assert.That(platform.nDisconnects, Is.EqualTo(1));
        }

        [Test]
        public async Task DisconnectAsync() {
            byte[][] expected = { new byte[] { 0xfe, 0x06 } };

            await debug.DisconnectAsync();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            Assert.That(platform.nDisconnects, Is.EqualTo(1));
        }

        [Test]
        public void ResetAfterGc() {
            byte[][] expected = { new byte[] { 0xfe, 0x05 } };

            debug.ResetAfterGc();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
