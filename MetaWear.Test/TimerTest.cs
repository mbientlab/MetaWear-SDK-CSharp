using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class TimerTestFixtureData {
        public static IEnumerable Params {
            get {
                yield return new TestFixtureData(false);
                yield return new TestFixtureData(true);
            }
        }
    }

    [Parallelizable]
    [TestFixtureSource(typeof(TimerTestFixtureData), "Params")]
    class TimerTest : UnitTestBase {
        private IGpio gpio;
        private IScheduledTask mwTask;
        private bool indefinite;

        public TimerTest(bool indefinite) : base(typeof(IGpio)) {
            this.indefinite = indefinite;
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            Action commands = () => {
                gpio.Pins[0].AbsoluteReference.Read();
                gpio.Pins[0].Adc.Read();
            };

            gpio = metawear.GetModule<IGpio>();
            mwTask = await (indefinite ? metawear.ScheduleAsync(3141, true, commands) : metawear.ScheduleAsync(3141, 59, true, commands));
        }

        [Test]
        public async Task ScheduleAndRemoveAsync() {
            byte[][] expected = {
                new byte[] { 0x0c, 0x02, 0x45, 0x0c, 0x00, 0x00, (byte) (indefinite ? 0xff : 0x3B), (byte) (indefinite ? 0xff : 0x00), 0x0 },
                new byte[] { 0x0a, 0x02, 0x0c, 0x06, 0x00, 0x05, 0xc6, 0x05 },
                new byte[] { 0x0a, 0x03, 0x00, 0xff, 0xff, 0x00, 0xff },
                new byte[] { 0x0a, 0x02, 0x0c, 0x06, 0x00, 0x05, 0xc7, 0x05 },
                new byte[] { 0x0a, 0x03, 0x00, 0xff, 0xff, 0x00, 0xff },
                new byte[] { 0x0c, 0x05, 0x0 },
                new byte[] { 0x0a, 0x04, 0x0 },
                new byte[] { 0x0a, 0x04, 0x1 }
            };
            platform.fileSuffix = "scheduled_task";
            if (!indefinite) {
                await metawear.SerializeAsync();
            }
            mwTask.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Start() {
            byte[] expected = new byte[] { 0x0c, 0x03, 0x0 };

            mwTask.Start();

            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void Stop() {
            byte[] expected = new byte[] { 0x0c, 0x04, 0x0 };

            mwTask.Stop();

            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void Timeout() {
            platform.maxTimers = 0;
            Assert.ThrowsAsync<TimeoutException>(async () => {
                try {
                    await metawear.ScheduleAsync(26535, false, () => { });
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }
    }
}
