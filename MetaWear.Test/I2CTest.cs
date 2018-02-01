using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.SerialPassthrough;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class I2CTest : UnitTestBase {
        private II2CDataProducer i2c;
        private ISerialPassthrough serialPassthrough;

        public I2CTest() : base(typeof(ISerialPassthrough)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            serialPassthrough = metawear.GetModule<ISerialPassthrough>();
            i2c = serialPassthrough.I2C(0xa, 0x1);
        }

        [Test]
        public void ReadWhoAmI() {
            byte[][] expected = { new byte[] { 0x0d, 0xc1, 0x1c, 0x0d, 0x0a, 0x01 } };
            i2c.Read(0x1c, 0xd);

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadWhoAmIDataAsync() {
            byte[] expected = new byte[] { 0x2a };
            byte[] actual = null;

            await i2c.AddRouteAsync(source => source.Stream(data => actual = data.Value<byte[]>()));
            platform.sendMockResponse(new byte[] { 0x0d, 0x81, 0x0a, 0x2a });

            platform.fileSuffix = "i2c_stream";
            await metawear.SerializeAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task DirectReadWhoAmIAsync() {
            byte[][] expected = { new byte[] { 0x0d, 0x81, 0x1c, 0x0d, 0xff, 0x01 } };

            var task = serialPassthrough.ReadI2CAsync(0x1c, 0x0d, 1);
            platform.sendMockResponse(new byte[] { 0x0d, 0x81, 0xff, 0x2a });
            await task;

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task DirectReadWhoAmIDataAsync() {
            byte[] expected = new byte[] { 0x2a };

            var task = serialPassthrough.ReadI2CAsync(0x1c, 0x0d, 1);
            platform.sendMockResponse(new byte[] { 0x0d, 0x81, 0xff, 0x2a });
            var actual = await task;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void DirectReadTimeout() {
            Assert.ThrowsAsync<TimeoutException>(async () => {
                try {
                    await serialPassthrough.ReadI2CAsync(0x1c, 0x0d, 1);
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }
    }
}
