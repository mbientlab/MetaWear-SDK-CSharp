using MbientLab.MetaWear.Core;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class SettingsRev1Test : UnitTestBase {
        private ISettings settings;

        public SettingsRev1Test() : base(typeof(ISettings)) {
            platform.initResponse.moduleResponses[0x11] = new byte[] { 0x11, 0x80, 0x00, 0x01 };
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            settings = metawear.GetModule<ISettings>();
        }

        [Test]
        public void SetConnParams() {
            byte[][] expected = { new byte[] { 0x11, 0x09, 0x58, 0x02, 0x20, 0x03, 0x80, 0x00, 0x66, 0x06 } };
            settings.EditBleConnParams(minConnInterval: 750f, maxConnInterval: 1000f, slaveLatency: 128, supervisorTimeout: 16384);

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void DisconnectEvent() {
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                try {
                    await settings.OnDisconnectAsync(() => { });
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
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
