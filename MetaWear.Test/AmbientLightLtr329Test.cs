using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.AmbientLightLtr329;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class AmbientLightLtr329TestDataClass {
        public static IEnumerable ConfigureTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach(var gain in Enum.GetValues(typeof(Gain))) {
                    foreach(var time in Enum.GetValues(typeof(IntegrationTime))) {
                        foreach(var rate in Enum.GetValues(typeof(MeasurementRate))) {
                            testCases.Add(new TestCaseData(gain, time, rate));
                        }
                    }
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class AmbientLightLtr329Test : UnitTestBase {
        private IAmbientLightLtr329 als;

        public AmbientLightLtr329Test() : base(typeof(IAmbientLightLtr329)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            als = metawear.GetModule<IAmbientLightLtr329>();
        }

        static readonly byte[] GAIN_BITMASKS = new byte[] { 0x0, 0x4, 0x8, 0xc, 0x18, 0x1c };
        static readonly byte[][] TIME_RATE_BITMASKS = {
            new byte[] { 0b00000000, 0b00000001, 0b00000010, 0b00000011, 0b00000100, 0b00000101 },
            new byte[] { 0b00001000, 0b00001001, 0b00001010, 0b00001011, 0b00001100, 0b00001101 },
            new byte[] { 0b00010000, 0b00010001, 0b00010010, 0b00010011, 0b00010100, 0b00010101 },
            new byte[] { 0b00011000, 0b00011001, 0b00011010, 0b00011011, 0b00011100, 0b00011101 },
            new byte[] { 0b00100000, 0b00100001, 0b00100010, 0b00100011, 0b00100100, 0b00100101 },
            new byte[] { 0b00101000, 0b00101001, 0b00101010, 0b00101011, 0b00101100, 0b00101101 },
            new byte[] { 0b00110000, 0b00110001, 0b00110010, 0b00110011, 0b00110100, 0b00110101 },
            new byte[] { 0b00111000, 0b00111001, 0b00111010, 0b00111011, 0b00111100, 0b00111101},
        };
        [TestCaseSource(typeof(AmbientLightLtr329TestDataClass), "ConfigureTestCases")]
        public void Configure(Gain gain, IntegrationTime time, MeasurementRate rate) {
            byte[][] expected =  {
                new byte[] {0x14, 0x02, GAIN_BITMASKS[(int) gain], TIME_RATE_BITMASKS[(int) time][(int) rate]},
            };

            als.Configure(gain: gain, time: time, rate: rate);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Start() {
            byte[][] expected = { new byte[] { 0x14, 0x01, 0x01 } };

            als.Illuminance.Start();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void Stop() {
            byte[][] expected = { new byte[] { 0x14, 0x01, 0x00 } };

            als.Illuminance.Stop();
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task CreateAndRemoveStreamAsync() {
            byte[][] expected = {
                new byte[] { 0x14, 0x03, 0x01 },
                new byte[] { 0x14, 0x03, 0x00 }
            };

            var route = await als.Illuminance.AddRouteAsync(source => source.Stream(null));
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleDataAsync() {
            float expected = 11571.949f, actual = 0;

            await als.Illuminance.AddRouteAsync(source => source.Stream(data => actual = data.Value<float>()));
            platform.sendMockResponse(new byte[] { 0x14, 0x03, 0xed, 0x92, 0xb0, 0x00 });

            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }
    }
}
