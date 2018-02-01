using MbientLab.MetaWear.Peripheral;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class HapticTest : UnitTestBase {
        private IHaptic haptic;

        public HapticTest() : base(typeof(IHaptic)) {

        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            haptic = metawear.GetModule<IHaptic>();
        }

        [Test]
        public void StartMotor() {
            byte[][] expected = { new byte[] { 0x08, 0x01, 0xf8, 0x88, 0x13, 0x00 } };

            haptic.StartMotor(5000, 100f);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void StartBuzzer() {
            byte[][] expected = { new byte[] { 0x08, 0x01, 0x7f, 0x4c, 0x1d, 0x01 } };

            haptic.StartBuzzer(7500);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
