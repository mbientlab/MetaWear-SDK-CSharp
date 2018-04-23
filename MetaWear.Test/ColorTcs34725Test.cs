using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.ColorTcs34725;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    public class ColorTcs34725TestDataClass {
        public static IEnumerable ConfigureTestCases {
            get {
                Tuple<float, byte>[] integrationTimes = new Tuple<float, byte>[] {
                    Tuple.Create(4.8f, (byte) 0xfe),
                    Tuple.Create(612f, (byte) 0x01),
                };
                Tuple<bool, byte>[] illuminator = new Tuple<bool, byte>[] {
                    Tuple.Create(true, (byte) 0x1),
                    Tuple.Create(false, (byte) 0x00),
                };

                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach (var mode in Enum.GetValues(typeof(Gain))) {
                    foreach (var time in integrationTimes) {
                        foreach(var led in illuminator) {
                            testCases.Add(new TestCaseData(mode, time.Item1, time.Item2, led.Item1, led.Item2));
                        }
                    }
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class ColorTcs34725Test : UnitTestBase {
        private IColorTcs34725 color;

        public ColorTcs34725Test() : base(typeof(IColorTcs34725)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            color = metawear.GetModule<IColorTcs34725>();
        }

        [TestCaseSource(typeof(ColorTcs34725TestDataClass), "ConfigureTestCases")]
        public void Configure(Gain gain, float time, byte timeMask, bool illuminator, byte illuminatorMask) {
            byte[][] expected = { new byte[] { 0x17, 0x02, timeMask, (byte) gain, illuminatorMask } };

            color.Configure(gain: gain, integationTime: time, illuminate: illuminator);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
        
        [Test]
        public async Task InterpretDataAsync() {
            Adc expected = new Adc(418, 123, 154, 124);
            Adc actual = null;

            await color.Adc.AddRouteAsync(source => source.Stream(data => actual = data.Value<Adc>()));

            platform.sendMockResponse(new byte[] { 0x17, 0x81, 0xa2, 0x01, 0x7b, 0x00, 0x9a, 0x00, 0x7c, 0x00 });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretSingleDataAsync() {
            ushort[] expected = new ushort[] { 418, 123, 154, 124 };
            ushort[] actual = new ushort[4];

            await color.Adc.AddRouteAsync(source =>
                source.Split().Index(0).Stream(data => actual[0] = data.Value<ushort>())
                        .Index(1).Stream(data => actual[1] = data.Value<ushort>())
                        .Index(2).Stream(data => actual[2] = data.Value<ushort>())
                        .Index(3).Stream(data => actual[3] = data.Value<ushort>())
            );
            
            platform.sendMockResponse(new byte[] { 0x17, 0x81, 0xa2, 0x01, 0x7b, 0x00, 0x9a, 0x00, 0x7c, 0x00 });
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
