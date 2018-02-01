using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.BarometerBosch;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    enum BarometerProducer {
        Pressure,
        Altitude
    }

    internal class BarometerBoschTestFixtureData {
        public static IEnumerable Params {
            get {
                yield return new TestFixtureData(typeof(IBarometerBme280));
                yield return new TestFixtureData(typeof(IBarometerBmp280));
            }
        }
    }

    public class BarometerBoschTestDataClass {
        public static IEnumerable ConfigureTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach (var mode in Enum.GetValues(typeof(Oversampling))) {
                    foreach (var coeff in Enum.GetValues(typeof(IirFilerCoeff))) {
                        testCases.Add(new TestCaseData(mode, coeff));
                    }
                }
                return testCases;
            }
        }

        public static IEnumerable ProducerTestCases {
            get {
                yield return new TestCaseData(BarometerProducer.Pressure);
                yield return new TestCaseData(BarometerProducer.Altitude);
            }
        }
    }

    [Parallelizable]
    [TestFixtureSource(typeof(BarometerBoschTestFixtureData), "Params")]
    class BarometerBoschTest : UnitTestBase {
        internal static readonly byte[] STANDBY_BITMASK = new byte[] { 0x00, 0x20, 0x40, 0x60, 0x80, 0xa0, 0xc0, 0xe0 };
        private IBarometerBosch barometer;

        public BarometerBoschTest(Type baroType) : base(baroType) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            barometer = metawear.GetModule<IBarometerBosch>();
        }

        [Test]
        public void StartNoAltitude() {
            byte[][] expected = { new byte[] { 0x12, 0x04, 0x01, 0x00 } };

            barometer.Start();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void StartWithAltitude() {
            byte[][] expected = { new byte[] { 0x12, 0x04, 0x01, 0x01 } };

            barometer.Altitude.Start();
            barometer.Start();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Stop() {
            byte[][] expected = { new byte[] { 0x12, 0x04, 0x00, 0x00 } };

            barometer.Stop();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        static readonly byte[] OS_BITMASK = new byte[] { 0x20, 0x24, 0x28, 0x2c, 0x30, 0x54 },
            FILTER_BITMASK = new byte[] { 0x00, 0x04, 0x08, 0x0c, 0x10 };
        [TestCaseSource(typeof(BarometerBoschTestDataClass), "ConfigureTestCases")]
        public void Configure(Oversampling os, IirFilerCoeff coeff) {
            byte[][] expected = { new byte[] { 0x12, 0x03, OS_BITMASK[(int) os], FILTER_BITMASK[(int) coeff] } };

            barometer.Configure(os, coeff);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(BarometerBoschTestDataClass), "ProducerTestCases")]
        public async Task CreateAndRemoveStreamAsync(BarometerProducer producer) {
            byte mask = (byte)((int)producer + 1);
            byte[][] expected = {
                new byte[] { 0x12, mask, 0x1 },
                new byte[] { 0x12, mask, 0x0 },
            };

            var route = await (producer == BarometerProducer.Pressure ? barometer.Pressure : barometer.Altitude)
                    .AddRouteAsync(source => source.Stream(null));
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(BarometerBoschTestDataClass), "ProducerTestCases")]
        public async Task InterpretData(BarometerProducer producer) {
            float expected = producer == BarometerProducer.Pressure ? 101173.828125f : -480.8828125f, actual = 0;
            byte[] response = producer == BarometerProducer.Pressure ?
                    new byte[] { 0x12, 0x01, 0xd3, 0x35, 0x8b, 0x01 } :
                    new byte[] { 0x12, 0x02, 0x1e, 0x1f, 0xfe, 0xff };

            var route = await (producer == BarometerProducer.Pressure ? barometer.Pressure : barometer.Altitude)
                    .AddRouteAsync(source => source.Stream(data => actual = data.Value<float>()));

            platform.sendMockResponse(response);
            Assert.That(actual, Is.EqualTo(expected).Within(0.00000001));
        }
    }
}
