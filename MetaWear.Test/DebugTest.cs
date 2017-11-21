using MbientLab.MetaWear.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [TestFixture]
    class DebugTest : UnitTestBase {
        private IDebug debug;

        public DebugTest() : base(typeof(IDebug)) { }

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
