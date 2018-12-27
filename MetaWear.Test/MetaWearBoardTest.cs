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

    [Parallelizable]
    [TestFixtureSource(typeof(MetaWearBoardTestFixtureData), "Params")]
    class MetaWearBoardTest : UnitTestBase {
        private Model model;

        public MetaWearBoardTest(Model model) : base(model) {
            this.model = model;
        }

        [Test]
        public async Task SerializeAsync() {
            platform.fileSuffix = string.Format("{0}_serialize_test", model.ToString());
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
                var copy = new Dictionary<byte, byte[]>(platform.initResponse.moduleResponses);
                try {
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

        [Test]
        public async Task RetryServiceDiscovery() {
            platform.delayModuleDiscovery = true;
            var copy = platform.initResponse.moduleResponses[0xf];

            try {
                platform.initResponse.moduleResponses.Remove(0xf);
                await metawear.InitializeAsync();
            } catch (TimeoutException) {
                platform.initResponse.moduleResponses[0xf] = copy;
            } finally {
                byte[][] checkpoint1 = {
                    new byte[] {0x01, 0x80}, new byte[] {0x02, 0x80}, new byte[] {0x03, 0x80}, new byte[] {0x04, 0x80},
                    new byte[] {0x05, 0x80}, new byte[] {0x06, 0x80}, new byte[] {0x07, 0x80}, new byte[] {0x08, 0x80},
                    new byte[] {0x09, 0x80}, new byte[] {0x0a, 0x80}, new byte[] {0x0b, 0x80}, new byte[] {0x0c, 0x80},
                    new byte[] {0x0d, 0x80}, new byte[] {0x0f, 0x80}
                };
                Assert.That(platform.GetConnectCommands(), Is.EqualTo(checkpoint1));

                platform.connectCommands.Clear();
                await metawear.InitializeAsync();

                byte[][] checkpoint2 = {
                    new byte[] {0x0f, 0x80}, new byte[] {0x10, 0x80}, new byte[] {0x11, 0x80},
                    new byte[] {0x12, 0x80}, new byte[] {0x13, 0x80}, new byte[] {0x14, 0x80}, new byte[] {0x15, 0x80},
                    new byte[] {0x16, 0x80}, new byte[] {0x17, 0x80}, new byte[] {0x18, 0x80}, new byte[] {0x19, 0x80},
                    new byte[] {0xfe, 0x80}
                };
                Assert.That(platform.GetConnectCommands(), Is.EqualTo(checkpoint2));
            }
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

            platform = new NunitPlatform(new InitializeResponse("1.3.4", typeof(IGpio), typeof(ILogging))) {
                fileSuffix = "scheduled_task"
            };

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
    
    [TestFixture]
    class ModuleInfoDumpTest : UnitTestBase {
        static readonly Dictionary<String, Dictionary<String, object>> fullExpected = new Dictionary<string, Dictionary<string, object>>() {
            {"Switch", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"Led", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"Accelerometer", new Dictionary<string, object>() { {"implementation", 1 }, {"revision", 1 } } },
            {"Temperature", new Dictionary<string, object>() { {"implementation", 1 }, {"revision", 0 }, { "extra", "[0x00, 0x03, 0x01, 0x02]" } } },
            {"Gpio", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 2 }, { "extra", "[0x03, 0x03, 0x03, 0x03, 0x01, 0x01, 0x01, 0x01]" } } },
            {"NeoPixel", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"IBeacon", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"Haptic", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"DataProcessor", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 3 }, { "extra", "[0x1c]" } } },
            {"Event", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 }, { "extra", "[0x1c]" } } },
            {"Logging", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 2 }, { "extra", "[0x08, 0x80, 0x2b, 0x00, 0x00]" } } },
            {"Timer", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 }, { "extra", "[0x08]" } } },
            {"SerialPassthrough", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 1 } } },
            {"Macro", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 1 }, { "extra", "[0x08]" } } },
            {"Conductance", new Dictionary<string, object>() },
            {"Settings", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"Barometer", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"Gyro", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 1 } } },
            {"AmbientLight", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } },
            {"Magnetometer", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 1 } } },
            {"Humidity", new Dictionary<string, object>() },
            {"Color", new Dictionary<string, object>() },
            {"Proximity", new Dictionary<string, object>() },
            {"SensorFusion", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 }, { "extra", "[0x03, 0x00, 0x06, 0x00, 0x02, 0x00, 0x01, 0x00]" } } },
            {"Debug", new Dictionary<string, object>() { {"implementation", 0 }, {"revision", 0 } } }
        };
        static string ToString(IDictionary dic) {
            var values = new List<String>();
            foreach(var k in dic.Keys) {
                values.Add(string.Format(dic[k] is string ? "{0}: \"{1}\"" : "{0}: {1}", k as string, dic[k]));
            }
            return string.Format("{{{0}{1}", string.Join(",", values), "}");
        }

        public ModuleInfoDumpTest() : base(Model.MetaMotionR) {

        }

        private void CompareDictionary(IDictionary actual, IDictionary expected) {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            foreach (var k in actual.Keys) {
                Assert.That(expected.Contains(k), Is.True);
            }
            foreach (var k in expected.Keys) {
                Assert.That(actual.Contains(k), Is.True);
            }

            foreach (var k in actual.Keys) {
                if (actual[k] is IDictionary && expected[k] is IDictionary) {
                    CompareDictionary(actual[k] as IDictionary, expected[k] as IDictionary);
                } else {
                    Assert.That(actual[k], Is.EqualTo(expected[k]));
                }
            }
        }

        [Test]
        public async Task FullDump() {
            platform.delayModuleDiscovery = true;
            var result = await metawear.GetModuleInfoAsync(null);

            CompareDictionary(result, fullExpected);
        }

        [Test]
        public async Task PartialDump() {
            byte[][] expected = {
                new byte[] {0x01, 0x80}, new byte[] {0x02, 0x80},
                new byte[] {0x03, 0x80}, new byte[] {0x04, 0x80},
                new byte[] {0x05, 0x80}, new byte[] {0x06, 0x80},
                new byte[] {0x07, 0x80}, new byte[] {0x08, 0x80},
                new byte[] {0x12, 0x80}, new byte[] {0x13, 0x80},
                new byte[] {0x14, 0x80}, new byte[] {0x15, 0x80},
                new byte[] {0x16, 0x80}, new byte[] {0x17, 0x80},
                new byte[] {0x18, 0x80}, new byte[] {0x19, 0x80},
                new byte[] {0xfe, 0x80}
            };

            IDictionary partial = new Dictionary<String, Dictionary<String, object>>();
            foreach (String k in new String[] { "DataProcessor", "Event", "Logging", "Timer", "SerialPassthrough", "Macro", "Conductance", "Settings" }) {
                partial.Add(k, fullExpected[k]);
            }

            platform.delayModuleDiscovery = true;
            platform.connectCommands.Clear();
            await metawear.GetModuleInfoAsync(partial);

            Assert.That(platform.GetConnectCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Timeout() {
            Assert.ThrowsAsync<TaskTimeoutException>(async () => {
                var copy = new Dictionary<byte, byte[]>(platform.initResponse.moduleResponses);
                try {
                    platform.initResponse.moduleResponses.Clear();
                    await metawear.GetModuleInfoAsync(null);
                } catch (TaskTimeoutException e) {
                    foreach (var it in copy) {
                        platform.initResponse.moduleResponses.Add(it.Key, it.Value);
                    }
                    throw e;
                }
            });
        }

        [Test]
        public async Task CheckPartialResult() {
            var copy = new Dictionary<byte, byte[]>(platform.initResponse.moduleResponses);
            var remove = new List<byte>();

            foreach(var k in platform.initResponse.moduleResponses.Keys) {
                if (k > 8) {
                    remove.Add(k);
                }
            }
            foreach(var k in remove) {
                platform.initResponse.moduleResponses.Remove(k);
            }

            IDictionary partial = null;
            try {
                await metawear.GetModuleInfoAsync(null);
            } catch (TaskTimeoutException e) {
                partial = e.PartialResult as IDictionary;

                platform.initResponse.moduleResponses.Clear();
                foreach (var it in copy) {
                    platform.initResponse.moduleResponses.Add(it.Key, it.Value);
                }
            } finally {
                Assert.That(partial.Count, Is.EqualTo(8));
                foreach(var k in partial.Keys) {
                    Assert.That(ToString(partial[k] as IDictionary), Is.EqualTo(ToString(fullExpected[k as string])));
                }
            }
        }
    }
}
