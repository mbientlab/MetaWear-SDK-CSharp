using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.BarometerBmp280;

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class BarometerBmp280StandbyTestDataClass {
        public static IEnumerable StandbyTimeRawTestCases {
            get {
                float[] times = new float[] { 0.25f, 60.125f, 127f, 225f, 376, 1234, 2718.2818f, 3141.592653f };

                List<TestCaseData> testCases = new List<TestCaseData>();
                for (int i = 0; i < times.Length; i++) {
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
    class BarometerBmp280StandbyTest : UnitTestBase {
        private IBarometerBmp280 barometer;

        public BarometerBmp280StandbyTest() : base(typeof(IBarometerBmp280)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            barometer = metawear.GetModule<IBarometerBmp280>();
        }

        [TestCaseSource(typeof(BarometerBmp280StandbyTestDataClass), "StandbyTimeTestCases")]
        public void SetStandbyTime(StandbyTime time) {
            byte[][] expected = { new byte[] { 0x12, 0x03, 0x2c, BarometerBoschTest.STANDBY_BITMASK[(int)time] } };

            barometer.Configure(standbyTime: time);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(BarometerBmp280StandbyTestDataClass), "StandbyTimeRawTestCases")]
        public void SetStandbyTimeRaw(float time, byte mask) {
            byte[][] expected = { new byte[] { 0x12, 0x03, 0x2c, mask } };

            barometer.Configure(standbyTime: time);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
