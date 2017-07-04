using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class MetaWearBoardTestFixtureData {
        public static IEnumerable Params {
            get {
                foreach (var model in Enum.GetValues(typeof(Model))) {
                    yield return new TestFixtureData(model);
                }
            }
        }
    }

    [TestFixtureSource(typeof(MetaWearBoardTestFixtureData), "Params")]
    class MetaWearBoardTest : UnitTestBase {
        private Model model;

        public MetaWearBoardTest(Model model) : base(model) {
            this.model = model;
        }

        [Test]
        public async Task SerializeAsync() {
            await metawear.SerializeAsync();
        }

        [Test]
        public void CheckModel() {
            Assert.That(metawear.Model, Is.EqualTo(model));
        }

        [Test]
        public void TearDown() {
            byte[][] expected = {
                new byte[] {0x09, 0x08},
                new byte[] {0x0a, 0x05},
                new byte[] {0x0b, 0x0a},
                new byte[] {0x0c, 0x05, 0x00},
                new byte[] {0x0c, 0x05, 0x01},
                new byte[] {0x0c, 0x05, 0x02},
                new byte[] {0x0c, 0x05, 0x03},
                new byte[] {0x0c, 0x05, 0x04},
                new byte[] {0x0c, 0x05, 0x05},
                new byte[] {0x0c, 0x05, 0x06},
                new byte[] {0x0c, 0x05, 0x07}
            };

            metawear.TearDown();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [TestFixture]
    class MetaWearBoardInterfaceTest : UnitTestBase {
        [Test]
        public async Task ReadDeviceInfoAsync() {
            DeviceInformation expected = new DeviceInformation("MbientLab Inc", "deadbeef", "003BF9", "1.2.5", "0.3");
            var actual = await metawear.ReadDeviceInformationAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadBatteryLevelAsync() {
            Assert.That(await metawear.ReadBatteryLevelAsync(), Is.EqualTo(99));
        }
    }
}
