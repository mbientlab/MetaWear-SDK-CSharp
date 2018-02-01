using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;
using NUnit.Framework;
using System.Collections;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class LedPatternTestFixtureData {
        public static IEnumerable Params {
            get {
                yield return new TestFixtureData(true);
                yield return new TestFixtureData(false);
            }
        }
    }

    public class LedPatternTestDataClass {
        public static IEnumerable TestCases {
            get {
                yield return new TestCaseData(new byte[] { 0x02, 0x03, 0x00, 0x02, 0x1f, 0x00, 0x00, 0x00, 0x32, 0x00, 0x00, 0x00, 0xf4, 0x01, 0x00, 0x00, 0x0a }, 
                    Color.Green, Pattern.Blink, (ushort) 5000, (byte) 10);
                yield return new TestCaseData(new byte[] { 0x02, 0x03, 0x01, 0x02, 0x1f, 0x1f, 0x00, 0x00, 0xf4, 0x01, 0x00, 0x00, 0xE8, 0x03, 0x00, 0x00, 0x14 },
                    Color.Red, Pattern.Solid, (ushort) 10000, (byte) 20);
                yield return new TestCaseData(new byte[] { 0x02, 0x03, 0x02, 0x02, 0x1f, 0x00, 0xd5, 0x02, 0xf4, 0x01, 0xd5, 0x02, 0xd0, 0x07, 0x00, 0x00, 0x28 },
                    Color.Blue, Pattern.Pulse, (ushort) 10000, (byte) 40);
            }
        }
    }

    [Parallelizable]
    [TestFixtureSource(typeof(LedPatternTestFixtureData), "Params")]
    internal class LedPatternTest : UnitTestBase {
        private bool delaySupported;
        private ILed led;

        public LedPatternTest(bool delaySupported) : base(typeof(ILed)) {
            this.delaySupported = delaySupported;
            if (this.delaySupported) {
                platform.initResponse.moduleResponses[0x02] = new byte[] { 0x02, 0x80, 0x00, 0x01, 0x03, 0x00 };
            }
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            led = metawear.GetModule<ILed>();
        }

        [Test, TestCaseSource(typeof(LedPatternTestDataClass), "TestCases")]
        public void SetPresetPattern(byte[] expected, Color color, Pattern preset, ushort delay, byte repeat) {
            if (delaySupported) {
                expected[15] = (byte)((delay >> 8) & 0xff);
                expected[14] = (byte)(delay & 0xff);
            }

            led.EditPattern(color, preset, delay, repeat);

            byte[][] actual = platform.GetCommands();
            Assert.That(new byte[][] { expected }, Is.EqualTo(actual));
        }
    }
}
