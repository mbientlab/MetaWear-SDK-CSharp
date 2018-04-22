using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.BarometerBme280;

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class BarometerBme280StandbyTestDataClass {
        public static IEnumerable StandbyTimeRawTestCases {
            get {
                float[] times = new float[] { 0.25f, 60.125f, 127f, 225f, 376, 1234, 14.1421356f, 17.320508f };

                List<TestCaseData> testCases = new List<TestCaseData>();
                for(int i = 0; i < times.Length; i++) {
                    testCases.Add(new TestCaseData(times[i], BarometerBoschTest.STANDBY_BITMASK[i]));
                }
                return testCases;
            }
        }

        public static IEnumerable StandbyTimeTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach (var time in Enum.GetValues(typeof(StandbyTime))) {
                    testCases.Add(new TestCaseData(time));
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class BarometerBme280StandbyTest : UnitTestBase {
        private IBarometerBme280 barometer;

        public BarometerBme280StandbyTest() : base(typeof(IBarometerBme280)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            barometer = metawear.GetModule<IBarometerBme280>();
        }

        [TestCaseSource(typeof(BarometerBme280StandbyTestDataClass), "StandbyTimeTestCases")]
        public void SetStandbyTime(StandbyTime time) {
            byte[][] expected = { new byte[] { 0x12, 0x03, 0x2c, BarometerBoschTest.STANDBY_BITMASK[(int)time] } };

            barometer.Configure(standbyTime: time);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(BarometerBme280StandbyTestDataClass), "StandbyTimeRawTestCases")]
        public void SetStandbyTimeRaw(float time, byte mask) {
            byte[][] expected = { new byte[] { 0x12, 0x03, 0x2c, mask } };

            barometer.Configure(standbyTime: time);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
