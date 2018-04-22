using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.IBeacon;
using MbientLab.MetaWear.Sensor;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class IBeaconTest : UnitTestBase {
        private IIBeacon ibeacon;

        public IBeaconTest() : base(typeof(ISwitch), typeof(IAccelerometerBmi160), typeof(IIBeacon), typeof(IDataProcessor)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            ibeacon = metawear.GetModule<IIBeacon>();
        }

        [Test]
        public async Task SetMajorFeedbackAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x01, 0x01, 0xff, 0x00, 0x02, 0x13},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x00, 0x07, 0x03, 0x02, 0x09, 0x00},
                new byte[] {0x0a, 0x03, 0x00, 0x00},
                new byte[] {0x07, 0x01, 0x01}
            };

            var iswitch = metawear.GetModule<ISwitch>();
            await iswitch.State.AddRouteAsync(source => source.Count().React(token => ibeacon.Configure(majorToken: token)));
            ibeacon.Enable();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task SetSlicedFeedback() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x0a, 0x02, 0x03, 0x04, 0xff, 0x07, 0x03, 0x02, 0x09, 0x00},
                new byte[] { 0x0a, 0x03, 0x00, 0x00},
                new byte[] { 0x0a, 0x02, 0x03, 0x04, 0xff, 0x07, 0x04, 0x02, 0x45, 0x00},
                new byte[] { 0x0a, 0x03, 0x00, 0x00},
                new byte[] { 0x07, 0x01, 0x01}
            };

            var accelerometer = metawear.GetModule<IAccelerometer>();

            await accelerometer.Acceleration.AddRouteAsync(source => source.React(token => {
                ibeacon.Configure(majorToken: token.Slice(0, 4), minorToken: token.Slice(4, 2));
            }));
            ibeacon.Enable();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SliceOutOfBoundsException() {
            var accelerometer = metawear.GetModule<IAccelerometer>();
            Assert.ThrowsAsync<IndexOutOfRangeException>(async () => {
                await accelerometer.Acceleration.AddRouteAsync(source => source.React(token => {
                    ibeacon.Configure(majorToken: token.Slice(1, 6));
                }));
            });
        }

        [Test]
        public void SetMinor() {
            byte[][] expected = {
                new byte[] {0x07, 0x04, 0x1d, 0x1d}
            };

            ibeacon.Configure(minor: 7453);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetMajor() {
            byte[][] expected = {
                new byte[] { 0x07, 0x03, 0x4e, 0x00 }
            };

            ibeacon.Configure(major: 78);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetPeriod() {
            byte[][] expected = {
                new byte[] { 0x07, 0x07, 0xb3, 0x3a }
            };

            ibeacon.Configure(period: 15027);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetRxPower() {
            byte[][] expected = {
                new byte[] { 0x07, 0x05, 0xc9 }
            };

            ibeacon.Configure(rxPower: -55);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetTxPower() {
            byte[][] expected = {
                new byte[] { 0x07, 0x06, 0xf4 }
            };

            ibeacon.Configure(txPower: -12);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SetUuid() {
            byte[][] expected = {
                new byte[] { 0x07, 0x02, 0x5a, 0xe7, 0xba, 0xfb, 0x4c, 0x46, 0xdd, 0xd9, 0x95, 0x91, 0xcb, 0x85, 0x06, 0x90, 0x6a, 0x32 }
            };

            ibeacon.Configure(uuid: new System.Guid("326a9006-85cb-9195-d9dd-464cfbbae75a"));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadConfigAsync() {
            platform.customResponses.Add(new byte[] { 0x07, 0x82 },
                    new byte[] { 0x07, 0x82, 0x5a, 0xe7, 0xba, 0xfb, 0x4c, 0x46, 0xdd, 0xd9, 0x95, 0x91, 0xcb, 0x85, 0x00, 0x90, 0x6a, 0x32 });
            platform.customResponses.Add(new byte[] { 0x07, 0x83 },
                    new byte[] { 0x07, 0x83, 0x45, 0x0c });
            platform.customResponses.Add(new byte[] { 0x07, 0x84 },
                    new byte[] { 0x07, 0x84, 0x81, 0xe7 });
            platform.customResponses.Add(new byte[] { 0x07, 0x85 },
                    new byte[] { 0x07, 0x85, 0xc9 });
            platform.customResponses.Add(new byte[] { 0x07, 0x86 },
                    new byte[] { 0x07, 0x86, 0x00 });
            platform.customResponses.Add(new byte[] { 0x07, 0x87 },
                    new byte[] { 0x07, 0x87, 0x64, 0x00 });

            var expected = new Configuration(new System.Guid("326a9000-85cb-9195-d9dd-464cfbbae75a"), 3141, 59265, 100, -55, 0);

            Assert.That(await ibeacon.ReadConfigAsync(), Is.EqualTo(expected));
        }
    }
}
