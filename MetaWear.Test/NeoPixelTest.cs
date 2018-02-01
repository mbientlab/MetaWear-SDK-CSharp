using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.NeoPixel;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class NeoPixelTestDataClass {
        public static IEnumerable InitTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach (var ordering in Enum.GetValues(typeof(ColorOrdering))) {
                    foreach (var speed in Enum.GetValues(typeof(StrandSpeed))) {
                        testCases.Add(new TestCaseData(ordering, speed));
                    }
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class NeoPixelTest : UnitTestBase {
        private INeoPixel neopixel;

        public NeoPixelTest() : base(typeof(INeoPixel)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            neopixel = metawear.GetModule<INeoPixel>();
        }

        static byte[][] INIT_MASKS = {
            new byte[] {0b00000000, 0b00000100},
            new byte[] {0b00000001, 0b00000101},
            new byte[] {0b00000010, 0b00000110},
            new byte[] {0b00000011, 0b00000111},
        };
        [TestCaseSource(typeof(NeoPixelTestDataClass), "InitTestCases")]
        public void Init(ColorOrdering ordering, StrandSpeed speed) {
            byte[][] expected = {
                new byte[] { 0x06, 0x01, 0x01, INIT_MASKS[(int) ordering][(int) speed], 0x00, 0x1e },
                new byte[] { 0x06, 0x06, 0x01 }
            };
            var strand = neopixel.InitializeStrand(1, ordering, speed, 0, 30);
            strand.Free();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Rotate() {
            byte[] expected = new byte[] { 0x06, 0x05, 0x02, 0x01, 0x4b, 0xE8, 0x03 };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.Rotate(RotationDirection.Away, 1000, 75);
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void RotateIndefinitely() {
            byte[] expected = new byte[] { 0x06, 0x05, 0x02, 0x00, 0xff, 0xfa, 0x00 };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.Rotate(RotationDirection.Towards, 250);
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void StopRotate() {
            byte[] expected = new byte[] { 0x06, 0x05, 0x02, 0x00, 0x00, 0x00, 0x00 };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.StopRotation();
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void TurnOff() {
            byte[] expected = new byte[] { 0x06, 0x03, 0x02, 0x0a, 0x2d };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.Clear(10, 45);
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void SetColor() {
            byte[] expected = new byte[] { 0x06, 0x04, 0x02, 0x18, 0xd5, 0x55, 0x6b };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.SetRgb(24, 213, 85, 107);
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void Hold() {
            byte[] expected = new byte[] { 0x06, 0x02, 0x02, 0x01 };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.Hold();
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public void Release() {
            byte[] expected = new byte[] { 0x06, 0x02, 0x02, 0x00 };
            var strand = neopixel.InitializeStrand(2, ColorOrdering.WS2811_RBG, StrandSpeed.Fast, 1, 60);

            strand.Release();
            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }
    }
}
