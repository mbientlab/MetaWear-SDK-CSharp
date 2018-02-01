using MbientLab.MetaWear.Peripheral;

using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    internal class LedTest : UnitTestBase {
        private ILed led;

        public LedTest() : base(typeof(ILed)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            led = metawear.GetModule<ILed>();
        }

        [Test]
        public void Play() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x02, 0x01, 0x01 }
            };

            led.Play();

            byte[][] actual = platform.GetCommands();
            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void AutoPlay() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x02, 0x01, 0x02 }
            };

            led.AutoPlay();

            byte[][] actual = platform.GetCommands();
            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void Pause() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x02, 0x01, 0x00 }
            };

            led.Pause();

            byte[][] actual = platform.GetCommands();
            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void StopAndClear() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x02, 0x02, 0x01 }
            };

            led.Stop(true);

            byte[][] actual = platform.GetCommands();
            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void StopNoClear() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x02, 0x02, 0x0 }
            };

            led.Stop(false);

            byte[][] actual = platform.GetCommands();
            Assert.That(expected, Is.EqualTo(actual));
        }
    }
}
