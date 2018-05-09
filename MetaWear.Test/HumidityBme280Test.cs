using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.BarometerBosch;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class HumidityBme280TestDataClass {
        public static IEnumerable OversamplingTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                byte i = 0;
                foreach (var mode in Enum.GetValues(typeof(Oversampling))) {
                    testCases.Add(new TestCaseData(mode, i));
                    i++;
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class HumidityBme280Test : UnitTestBase {
        private IHumidityBme280 humidity;

        public HumidityBme280Test() : base(typeof(IHumidityBme280)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            humidity = metawear.GetModule<IHumidityBme280>();
        }

        [TestCaseSource(typeof(HumidityBme280TestDataClass), "OversamplingTestCases")]
        public void Configure(Oversampling os, byte mask) {
            byte[][] expected = { new byte[] { 0x16, 0x2, mask } };

            humidity.Configure(os);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretDataAsync() {
            float expected = 63.1943359375f;
            float actual = 0;

            await humidity.Percentage.AddRouteAsync(source => source.Stream(data => actual = data.Value<float>()));

            platform.sendMockResponse(new byte[] { 0x16, 0x81, 0xc7, 0xfc, 0x00, 0x00 });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task SerializeModule() {
            platform.fileSuffix = "humidity280_route";
            // Have to add route to trigger SerializationException  
            // Weird because it should be thrown without a route
            await humidity.Percentage.AddRouteAsync(source => source.Stream());
            await metawear.SerializeAsync();

            Assert.That(true);
        }
    }
}
