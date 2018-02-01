using MbientLab.MetaWear.Core;
using NUnit.Framework;
using System.Collections;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class SettingsTestFixtureData {
        public static IEnumerable Params {
            get {
                for(byte i = 0; i <= 6; i++) {
                    yield return new TestFixtureData(i);
                }
            }
        }
    }

    [Parallelizable]
    [TestFixtureSource(typeof(SettingsTestFixtureData), "Params")]
    class SettingsTest : UnitTestBase {
        private ISettings settings;
        private byte revision;

        public SettingsTest(byte revision) : base(typeof(ISettings)) {
            this.revision = revision;
            platform.initResponse.moduleResponses[0x11] = new byte[] { 0x11, 0x80, 0x00, revision };
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            settings = metawear.GetModule<ISettings>();
        }

        [Test]
        public void SetName() {
            byte[][] expected = { new byte[] { 0x11, 0x01, 0x41, 0x6e, 0x74, 0x69, 0x57, 0x61, 0x72, 0x65 } };

            settings.EditBleAdConfig(name: "AntiWare");
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetTxPower() {
            byte[][] expected = { new byte[] { 0x11, 0x03, 0xec } };

            settings.SetTxPower(-20);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetScanResponse() {
            byte[][] expected = {
                new byte[] {0x11, 0x08, 0x03, 0x03, 0xd8, 0xfe, 0x10, 0x16, 0xd8, 0xfe, 0x00, 0x12, 0x00, 0x6d, 0x62},
                new byte[] {0x11, 0x07, 0x69, 0x65, 0x6e, 0x74, 0x6c, 0x61, 0x62, 0x00}
            };

            settings.EditBleAdConfig(scanResponse: new byte[] { 0x03, 0x03, 0xD8, 0xfe, 0x10, 0x16, 0xd8, 0xfe, 0x00, 0x12, 0x00, 0x6d, 0x62, 0x69, 0x65, 0x6e, 0x74, 0x6c, 0x61, 0x62, 0x00 });
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetAdInterval() {
            byte[][] expected;

            if (revision >= 6) {
                expected = new byte[][] 
                    { new byte[] { 0x11, 0x02, 0x9b, 0x02, 0xb4, 0x00 }
                };
            } else if (revision >= 1) {
                expected = new byte[][] {
                    new byte[] { 0x11, 0x02, 0x9b, 0x02, 0xb4 }
                };
            } else {
                expected = new byte[][] {
                    new byte[] { 0x11, 0x02, 0xa1, 0x01, 0xb4 }
                };
            }

            settings.EditBleAdConfig(interval: 417, timeout: 180);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void StartAdvertising() {
            byte[][] expected = { new byte[] { 0x11, 0x5 } };

            settings.StartBleAdvertising();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
