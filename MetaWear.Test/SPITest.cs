using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.SerialPassthrough;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class SPITest : UnitTestBase {
        private ISPIDataProducer spi;
        private ISerialPassthrough serialPassthrough;

        public SPITest() : base(typeof(ISerialPassthrough)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            serialPassthrough = metawear.GetModule<ISerialPassthrough>();
            spi = serialPassthrough.SPI(0xe, 0x5);
        }

        [Test]
        public void ReadBmi160() {
            byte[][] expected = { new byte[] { 0x0d, 0xc2, 0x0a, 0x00, 0x0b, 0x07, 0x76, 0xe4, 0xda } };

            spi.Read(10, 0, 11, 7, 3, SpiFrequency._8_MHz, lsbFirst: false, data: new byte[] { 0xda });

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task Bmi160SpiDataAsync() {
            byte[] expected = new byte[] { 0x07, 0x30, 0x81, 0x0b, 0xc0 };
            byte[] actual = null;

            await spi.AddRouteAsync(source => source.Stream(data => actual = data.Value<byte[]>()));
            platform.sendMockResponse(new byte[] { 0x0d, 0x82, 0x0e, 0x07, 0x30, 0x81, 0x0b, 0xc0 });

            platform.fileSuffix = "spi_stream";
            await metawear.SerializeAsync();

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task DirectReadBmi160Data() {
            byte[] expected = new byte[] { 0x07, 0x30, 0x81, 0x0b, 0xc0 };
            var task = serialPassthrough.ReadSPIAsync(5, 10, 0, 11, 7, 3, SpiFrequency._8_MHz, lsbFirst: false, data: new byte[] { 0xda });
            platform.sendMockResponse(new byte[] { 0x0d, 0x82, 0x0f, 0x07, 0x30, 0x81, 0x0b, 0xc0 });

            var result = await task;

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void DirectReadBmi160Timeout() {
            Assert.ThrowsAsync<TimeoutException>(async () => {
                try {
                    await serialPassthrough.ReadSPIAsync(5, 10, 0, 11, 7, 3, SpiFrequency._8_MHz, lsbFirst: false, data: new byte[] { 0xda });
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }
    }
}
