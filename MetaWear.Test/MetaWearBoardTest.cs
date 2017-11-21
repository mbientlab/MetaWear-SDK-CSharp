using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
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
        [SetUp]
        public override Task SetUp() {
            metawear = new MetaWearBoard(platform, platform);
            return Task.FromResult(true);
        }

        [Test]
        public async Task ReadDeviceInfoAsync() {
            await metawear.InitializeAsync();

            DeviceInformation expected = new DeviceInformation("MbientLab Inc", "deadbeef", "003BF9", "1.2.5", "0.3");
            var actual = await metawear.ReadDeviceInformationAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadBatteryLevelAsync() {
            await metawear.InitializeAsync();
            Assert.That(await metawear.ReadBatteryLevelAsync(), Is.EqualTo(99));
        }

        [Test]
        public void InitTimeout() {
            Assert.ThrowsAsync<TimeoutException>(async () => {
                Dictionary<byte, byte[]> copy = null;
                try {
                    copy = new Dictionary<byte, byte[]>(platform.initResponse.moduleResponses);
                    platform.initResponse.moduleResponses.Clear();
                    await metawear.InitializeAsync();
                } catch (TimeoutException e) {
                    foreach(var it in copy) {
                        platform.initResponse.moduleResponses.Add(it.Key, it.Value);
                    }
                    throw e;
                }
            });
        }
    }

    [TestFixture]
    class MetaWearBoardDeserializeTest : UnitTestBase {
        [SetUp]
        public override Task SetUp() {
            return Task.FromResult(true);
        }

        [Test]
        public async Task NewFirmware() {
            byte[][] expected = {
                new byte[] { 0x01, 0x80 },
                new byte[] { 0x02, 0x80 },
                new byte[] { 0x03, 0x80 },
                new byte[] { 0x04, 0x80 },
                new byte[] { 0x05, 0x80 },
                new byte[] { 0x06, 0x80 },
                new byte[] { 0x07, 0x80 },
                new byte[] { 0x08, 0x80 },
                new byte[] { 0x09, 0x80 },
                new byte[] { 0x0a, 0x80 },
                new byte[] { 0x0b, 0x80 },
                new byte[] { 0x0c, 0x80 },
                new byte[] { 0x0d, 0x80 },
                new byte[] { 0x0f, 0x80 },
                new byte[] { 0x10, 0x80 },
                new byte[] { 0x11, 0x80 },
                new byte[] { 0x12, 0x80 },
                new byte[] { 0x13, 0x80 },
                new byte[] { 0x14, 0x80 },
                new byte[] { 0x15, 0x80 },
                new byte[] { 0x16, 0x80 },
                new byte[] { 0x17, 0x80 },
                new byte[] { 0x18, 0x80 },
                new byte[] { 0x19, 0x80 },
                new byte[] { 0xfe, 0x80 },
                new byte[] { 0x0b, 0x84 }
            };

            platform = new NunitPlatform(new InitializeResponse("1.3.4", typeof(IGpio), typeof(ILogging)));
            platform.fileSuffix = "scheduled_task";

            metawear = new MetaWearBoard(platform, platform);
            await metawear.DeserializeAsync();
            await metawear.InitializeAsync();

            Assert.That(platform.GetConnectCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task SameFirmware() {
            byte[][] expected = {
                new byte[] { 0x0b, 0x84 }
            };

            platform = new NunitPlatform(new InitializeResponse());
            platform.fileSuffix = "gpio_feedback";

            metawear = new MetaWearBoard(platform, platform);
            await metawear.DeserializeAsync();
            await metawear.InitializeAsync();

            Assert.That(platform.GetConnectCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task LongFirmwareAsync() {
            var firmware = "1.3.90";
            platform = new NunitPlatform(new InitializeResponse(firmware, typeof(IGpio), typeof(ILogging)));
            metawear = new MetaWearBoard(platform, platform);

            await metawear.InitializeAsync();
            var info = await metawear.ReadDeviceInformationAsync();

            Assert.That(info.FirmwareRevision, Is.EqualTo(firmware));
        }
    }
}
