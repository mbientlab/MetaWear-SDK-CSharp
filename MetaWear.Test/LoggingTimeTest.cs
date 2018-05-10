using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Sensor;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class LoggingTimeTest : UnitTestBase {
        protected ILogging logging;
        protected long now;

        public LoggingTimeTest() : base(typeof(IAccelerometerBmi160), typeof(ILogging)) {
        }

        [SetUp]
        public async override Task SetUp() {
            platform.customResponses.Add(new byte[] { 0x0b, 0x84 }, new byte[] { 0x0b, 0x84, 0xa9, 0x72, 0x04, 0x00, 0x01 });
            await base.SetUp();

            /*
            platform.fileSuffix = "logging_time_refs";
            await metawear.SerializeAsync();
            */

            now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //Console.WriteLine("what is now? " + now);
            logging = metawear.GetModule<ILogging>();
        }

        [Test]
        public async Task CheckRolloverAsync() {
            int actual = 0;
            DateTime? prev = null;
            var route = await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log(data => {
                if (prev != null) {
                    actual = Convert.ToInt32((data.Timestamp - prev.Value).TotalMilliseconds);
                }
                prev = data.Timestamp;
            }));

            var task = logging.DownloadAsync();

            platform.sendMockResponse(new byte[] { 0x0b, 0x84, 0x15, 0x04, 0x00, 0x00, 0x05 });
            platform.sendMockResponse(new byte[] { 11, 7,
                0xa1, 0xff, 0xff, 0xff, 0xff, 0x91, 0xef, 0, 0,
                0xa0, 0xff, 0xff, 0xff, 0xff, 0x80, 0xff, 0xb7, 0xff });
            platform.sendMockResponse(new byte[] { 11, 7, 0xa1, 13, 0, 0, 0, 116, 0xef, 0, 0, 0xa0, 13, 0, 0, 0, 125, 0xff, 0xba, 0xff });
            platform.sendMockResponse(new byte[] { 11, 8, 0, 0, 0, 0 });

            await task;

            Assert.That(actual, Is.EqualTo(21));
        }

        [Test]
        public virtual async Task HandlePastTime() {
            long? epoch = null;
            var route = await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log(data => {
                epoch = ((DateTimeOffset) data.Timestamp).ToUnixTimeMilliseconds();
            }));

            platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0x20, 0x75, 0x1b, 0x04, 0x00, 0x3e, 0x01, 0xcd, 0x01, 0x21, 0x76, 0x1b, 0x04, 0x00, 0xc0, 0x07, 0x00, 0x00 });
            // Allow a 1s leeway for when the unit test gets its `now` value
            Assert.That(epoch.Value, Is.EqualTo(now - 32701).Within(1000));
        }

        [Test]
        public virtual async Task HandleRolloverPastTime() {
            long? epoch = null;
            var route = await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log(data => {
                epoch = ((DateTimeOffset)data.Timestamp).ToUnixTimeMilliseconds();
            }));

            platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0x20, 0x00, 0xff, 0xff, 0xff, 0x3e, 0x01, 0xcd, 0x01, 0x21, 0x00, 0xff, 0xff, 0xff, 0xc0, 0x07, 0x00, 0x00 });
            // Allow a 1s leeway for when the unit test gets its `now` value
            Assert.That(epoch.Value, Is.EqualTo(now - 427372).Within(1000));

            platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0x20, 0xff, 0x00, 0x00, 0x00, 0x3e, 0x01, 0xcd, 0x01, 0x21, 0xff, 0x00, 0x00, 0x00, 0xc0, 0x07, 0x00, 0x00 });
            // Allow a 1s leeway for when the unit test gets its `now` value
            Assert.That(epoch.Value, Is.EqualTo(now - 426861).Within(1000));
        }
    }

    [Parallelizable]
    [TestFixture]
    class DeserializedLoggingTimeTest : LoggingTimeTest {
        public DeserializedLoggingTimeTest() : base() { }

        [SetUp]
        public async override Task SetUp() {
            platform.fileSuffix = "logging_time_refs";
            metawear = new MetaWearBoard(platform, platform);

            await metawear.DeserializeAsync();
            await metawear.InitializeAsync();
            
            logging = metawear.GetModule<ILogging>();
        }

        [Test]
        public override async Task HandleRolloverPastTime() {
            now = 1525978892783;
            await base.HandleRolloverPastTime();
        }

        [Test]
        public override async Task HandlePastTime() {
            now = 1525978892591;
            await base.HandlePastTime();
        }
    }
}
