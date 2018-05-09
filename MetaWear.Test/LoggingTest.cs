using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Sensor;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class LoggingTest : UnitTestBase {
        private ILogging logging;

        public LoggingTest() : base(typeof(IAccelerometerBmi160), typeof(ILogging), typeof(IDebug)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            logging = metawear.GetModule<ILogging>();
        }

        [Test]
        public void StartOverwrite() {
            byte[][] expected = {
                new byte[] { 0x0b, 0x0b, 0x01 },
                new byte[] { 0x0b, 0x01, 0x01 }
            };

            logging.Start(overwrite: true);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void StartNoOverwrite() {
            byte[][] expected = {
                new byte[] { 0x0b, 0x0b, 0x00 },
                new byte[] { 0x0b, 0x01, 0x01 }
            };

            logging.Start(overwrite: false);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Stop() {
            byte[][] expected = { new byte[] { 0x0b, 0x01, 0x00 } };

            logging.Stop();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void ClearEntries() {
            byte[][] expected = { new byte[] { 0x0b, 0x09, 0xff, 0xff, 0xff, 0xff } };

            logging.ClearEntries();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void TimeoutHandler() {
            platform.maxLoggers = 0;
            Assert.ThrowsAsync<TimeoutException>(async () => {
                try {
                    await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log());
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }

        [Test]
        public async Task ReadoutProgess() {
            uint[] expected = new uint[] {
                0x019e,
                0x0271, 0x0251, 0x0231, 0x0211, 0x01f1,
                0x01d1, 0x01b1, 0x0191, 0x0171, 0x0151,
                0x0131, 0x0111, 0x00f1, 0x00d1, 0x00b1,
                0x0091, 0x0071, 0x0051, 0x0031, 0x0011,
                0x0000
            };
            uint[] actual = new uint[22];
            int i = 0;

            byte[][] progressResponses = {
                new byte[] {0x0b, 0x08, 0x71, 0x02, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x51, 0x02, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x31, 0x02, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x11, 0x02, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0xf1, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0xd1, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0xb1, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x91, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x71, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x51, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x31, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x11, 0x01, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0xf1, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0xd1, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0xb1, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x91, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x71, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x51, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x31, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x11, 0x00, 0x00, 0x00},
                new byte[] {0x0b, 0x08, 0x00, 0x00, 0x00, 0x00}
            };

            var task = logging.DownloadAsync(22, (nEntries, totalEntries) => actual[i++] = nEntries);
            foreach(byte[] it in progressResponses) {
                platform.sendMockResponse(it);
            }

            await task;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Download() {
            byte[][] expected = {
                new byte[] {0x0b, 0x85},
                new byte[] {0x0b, 0x0d, 0x01},
                new byte[] {0x0b, 0x07, 0x01},
                new byte[] {0x0b, 0x08, 0x01},
                new byte[] {0x0b, 0x06, 0x9e, 0x01, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00},
                new byte[] { 0x0b, 0x0e }
            };

            logging.DownloadAsync(20, (nEntries, totalEntries) => { });
            platform.sendMockResponse(new byte[] { 0xb, 0xd });

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void InterruptDownload() {
            Assert.ThrowsAsync<IOException>(async () => {
                var task = logging.DownloadAsync(20, (nEntries, totalEntries) => { });
                new Timer(e => metawear.GetModule<IDebug>().DisconnectAsync(), null, 0, 5000L);
                await task;
            });
        }

        [Test]
        public void UnknownEntry() {
            Object[] expected = new Object[] { LogDownloadError.UNKNOWN_LOG_ENTRY, (byte)0x1, (ushort)0x016c };
            Object[] actual = new Object[3];

            logging.DownloadAsync((type, id, timestamp, data) => {
                actual[0] = type;
                actual[1] = id;
                actual[2] = BitConverter.ToUInt16(data, 0);
            });
            platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0xa1, 0xcc, 0x4d, 0x00, 0x00, 0x6c, 0x01, 0x00, 0x00 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task UnwantedProgressUpdate() {
            // receiving unwanted 'download completed' response was causing null pointer exception  
            // testing that code ignores this response
            platform.sendMockResponse(new byte[] { 0x0b, 0x08, 0x00, 0x00, 0x00, 0x00 });

            var task = logging.DownloadAsync(20, (nEntries, totalEntries) => { });
            platform.sendMockResponse(new byte[] { 0x0b, 0x08, 0x00, 0x00, 0x00, 0x00 });
            await task;

            // checking that code ignores this response after a download is completed
            platform.sendMockResponse(new byte[] { 0x0b, 0x08, 0x00, 0x00, 0x00, 0x00 });
        }
    }
}
