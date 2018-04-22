using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.ProximityTsl2671;

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class ProximityTsl2671TestDataClass {
        public static IEnumerable ConfigureTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();

                byte[] timeBitmasks = new byte[] { 0xfe, 0x1 };
                float[] times = new float[] { 5.44f, 693.6f };
                byte[] pulses = new byte[] { 0, 255 };
                foreach (var diode in Enum.GetValues(typeof(ReceiverDiode))) {
                    foreach (var current in Enum.GetValues(typeof(TransmitterDriveCurrent))) {
                        for(int i = 0; i < times.Length; i++) { 
                            foreach(var p in pulses) {
                                testCases.Add(new TestCaseData(diode, current, times[i], timeBitmasks[i], p));
                            }
                        }
                    }
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class ProximityTsl2671Test : UnitTestBase {
        private IProximityTsl2671 proximity;

        public ProximityTsl2671Test() : base(typeof(IProximityTsl2671)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            proximity = metawear.GetModule<IProximityTsl2671>();
        }

        static readonly byte[][] CURRENT_DIODE_BITMASKS = {
            new byte[] { 0b00010000, 0b00100000 , 0b00110000 },
            new byte[] { 0b01010000, 0b01100000 , 0b01110000 },
            new byte[] { 0b10010000, 0b10100000 , 0b10110000 },
            new byte[] { 0b11010000, 0b11100000 , 0b11110000 }
        };
        [TestCaseSource(typeof(ProximityTsl2671TestDataClass), "ConfigureTestCases")]
        public void Configure(ReceiverDiode diode, TransmitterDriveCurrent current, float time, byte timeMask, byte pulse) {
            byte[][] expected = {
                new byte[] { 0x18, 0x2, timeMask, pulse, CURRENT_DIODE_BITMASKS[(int) current][(int)diode] }
            };

            proximity.Configure(diode, current, time, pulse);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ReadAsync() {
            byte[][] expected = { new byte[] { 0x18, 0x81 } };

            await proximity.Adc.AddRouteAsync(source => source.Stream(null));
            proximity.Adc.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void SilentRead() {
            byte[][] expected = { new byte[] { 0x18, 0xc1 } };

            proximity.Adc.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretDataAsync() {
            ushort expected = 1522, actual = 0;

            await proximity.Adc.AddRouteAsync(source => source.Stream(data => actual = data.Value<ushort>()));
            platform.sendMockResponse(new byte[] { 0x18, 0x81, 0xf2, 0x05 });

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
