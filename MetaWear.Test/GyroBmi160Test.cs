using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Data;

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MbientLab.MetaWear.Sensor.GyroBmi160;

namespace MbientLab.MetaWear.Test {
    public class GyroBmi160ConfigTestDataClass {
        public static IEnumerable TestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach(var odr in Enum.GetValues(typeof(OutputDataRate))) {
                    foreach(var range in Enum.GetValues(typeof(DataRange))) {
                        testCases.Add(new TestCaseData(odr, range));
                    }
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class GyroBmi160Test : UnitTestBase {
        private IGyroBmi160 gyro;

        public GyroBmi160Test() : base(typeof(IGyroBmi160)) {
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            gyro = metawear.GetModule<IGyroBmi160>();
        }

        internal static readonly byte[] ODR_BITMASK = new byte[] { 0b0110, 0b0111, 0b1000, 0b1001, 0b1010, 0b1011, 0b1100, 0b1101 },
            RANGE_BITMASK = new byte[] { 0b000, 0b001, 0b010, 0b011, 0b100 };

        [TestCaseSource(typeof(GyroBmi160ConfigTestDataClass), "TestCases")]
        public void Configure(OutputDataRate odr, DataRange range) {
            byte[][] expected = { new byte[] { 0x13, 0x03, (byte)(0x20 | ODR_BITMASK[(int) odr]), RANGE_BITMASK[(int) range] } };

            gyro.Configure(odr, range);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task CreateAndRemoveStreamAsync() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x13, 0x05, 0x01 },
                new byte[] { 0x13, 0x02, 0x01, 0x00 },
                new byte[] { 0x13, 0x01, 0x01 },
                new byte[] { 0x13, 0x01, 0x00 },
                new byte[] { 0x13, 0x02, 0x00, 0x01 },
                new byte[] { 0x13, 0x05, 0x00 }
            };

            var route = await gyro.AngularVelocity.AddRouteAsync(source => source.Stream(null));
            gyro.AngularVelocity.Start();
            gyro.Start();

            gyro.Stop();
            gyro.AngularVelocity.Stop();
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretDataAsync() {
            AngularVelocity expected = new AngularVelocity(
                BitConverter.ToSingle(new byte[] { 0x4b, 0x34, 0x83, 0x43 }, 0),
                BitConverter.ToSingle(new byte[] { 0x9c, 0xbf, 0xf9, 0x43 }, 0),
                BitConverter.ToSingle(new byte[] { 0x90, 0xc1, 0xf9, 0xc3 }, 0)
            ), actual = null;

            gyro.Configure(range: DataRange._500dps);
            await gyro.AngularVelocity.AddRouteAsync(source => source.Stream(data => actual = data.Value<AngularVelocity>()));

            platform.sendMockResponse(new byte[] { 0x13, 0x05, 0x3e, 0x43, 0xff, 0x7f, 0x00, 0x80 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleSingleAxisDataAsync() {
            float[] expected = new float[] { 262.409f, 499.497f, -499.512f };
            float[] actual = new float[3];

            gyro.Configure(range: DataRange._500dps);
            await gyro.AngularVelocity.AddRouteAsync(source =>
                source.Split().Index(0).Stream(data => actual[0] = data.Value<float>())
                        .Index(1).Stream(data => actual[1] = data.Value<float>())
                        .Index(2).Stream(data => actual[2] = data.Value<float>())
            );

            platform.sendMockResponse(new byte[] { 0x13, 0x05, 0x3e, 0x43, 0xff, 0x7f, 0x00, 0x80 });
            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public async Task HandlePackedResponseAsync() {
            byte[] response = new byte[] {0x13, 0x07, 0x09, 0x15, 0xad, 0x26, 0x08, 0xde, 0x8a, 0x1a, 0x0d,
                0x26, 0x65, 0xe4, 0x8d, 0x20, 0xac, 0x27, 0x73, 0xec};
            AngularVelocity[] expected = new AngularVelocity[] {
                new AngularVelocity(
                    BitConverter.ToSingle(new byte[] {0x45, 0x2d, 0x24, 0x43 }, 0),
                    BitConverter.ToSingle(new byte[] {0x0d, 0xee, 0x96, 0x43 }, 0),
                    BitConverter.ToSingle(new byte[] {0x9c, 0x8f, 0x84, 0xc3 }, 0)
                ),
                new AngularVelocity(
                    BitConverter.ToSingle(new byte[] {0x58, 0x22, 0x4f, 0x43 }, 0),
                    BitConverter.ToSingle(new byte[] {0xa9, 0x7d, 0x94, 0x43 }, 0),
                    BitConverter.ToSingle(new byte[] {0x13, 0x75, 0x57, 0xc3 }, 0)
                ),
                new AngularVelocity(
                    BitConverter.ToSingle(new byte[] {0x0d, 0x0e, 0x7e, 0x43 }, 0),
                    BitConverter.ToSingle(new byte[] {0x2c, 0xd1, 0x9a, 0x43 }, 0),
                    BitConverter.ToSingle(new byte[] {0x6a, 0x97, 0x18, 0xc3 }, 0)
                )
            }, actual = new AngularVelocity[3];

            int i = 0;
            await gyro.PackedAngularVelocity.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<AngularVelocity>()));

            gyro.Configure(range: DataRange._1000dps);
            platform.sendMockResponse(response);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
