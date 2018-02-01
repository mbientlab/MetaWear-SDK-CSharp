using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class DebugRouteTest : UnitTestBase {
        private IDebug debug;

        public DebugRouteTest() : base(typeof(IDebug), typeof(ISwitch), typeof(IDataProcessor)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            debug = metawear.GetModule<IDebug>();
        }

        [Test]
        public async Task ResetRouteAsync() {
            byte[][] expected = {
                new byte[] { 0x09, 0x02, 0x01, 0x01, 0xff, 0x00, 0x06, 0x00, 0x01},
                new byte[] { 0x0a, 0x02, 0x09, 0x03, 0x00, 0xfe, 0x01, 0x00},
                new byte[] { 0x0a, 0x03}
            };

            await metawear.GetModule<ISwitch>().State.AddRouteAsync(source => source.Filter(Comparison.Eq, 1).React(token => debug.ResetAsync()));
            Assert.That(platform.nDisconnects, Is.EqualTo(0));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task JumoToBootloaderRouteAsync() {
            byte[][] expected = {
                new byte[] { 0x09, 0x02, 0x01, 0x01, 0xff, 0x00, 0x06, 0x00, 0x01},
                new byte[] { 0x0a, 0x02, 0x09, 0x03, 0x00, 0xfe, 0x02, 0x00},
                new byte[] { 0x0a, 0x03}
            };

            await metawear.GetModule<ISwitch>().State.AddRouteAsync(source => source.Filter(Comparison.Eq, 1).React(token => debug.JumpToBootloaderAsync()));
            Assert.That(platform.nDisconnects, Is.EqualTo(0));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task DisconnectRouteAsync() {
            byte[][] expected = {
                new byte[] { 0x09, 0x02, 0x01, 0x01, 0xff, 0x00, 0x06, 0x00, 0x01},
                new byte[] { 0x0a, 0x02, 0x09, 0x03, 0x00, 0xfe, 0x06, 0x00},
                new byte[] { 0x0a, 0x03}
            };

            await metawear.GetModule<ISwitch>().State.AddRouteAsync(source => source.Filter(Comparison.Eq, 1).React(token => debug.DisconnectAsync()));
            Assert.That(platform.nDisconnects, Is.EqualTo(0));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
