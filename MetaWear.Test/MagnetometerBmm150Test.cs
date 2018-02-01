using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.MagnetometerBmm150;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    internal class MagnetometerBmm150TestFixtureData {
        public static IEnumerable Params {
            get {
                yield return new TestFixtureData((byte) 1);
                yield return new TestFixtureData((byte) 2);
            }
        }
    }

    internal class MagnetometerBmm150TestDataClass {
        public static IEnumerable PresetTestCases {
            get {
                List<TestCaseData> testCases = new List<TestCaseData>();
                foreach (var preset in Enum.GetValues(typeof(Preset))) {
                    testCases.Add(new TestCaseData(new object[] { preset }));
                }
                return testCases;
            }
        }
    }

    [Parallelizable]
    [TestFixtureSource(typeof(MagnetometerBmm150TestFixtureData), "Params")]
    class MagnetometerBmm150Test : UnitTestBase {
        private IMagnetometerBmm150 magnetometer;
        private byte revision;

        public MagnetometerBmm150Test(byte revision) : base(typeof(IMagnetometerBmm150)) {
            this.revision = revision;
            platform.initResponse.moduleResponses[0x15] = new byte[] { 0x15, 0x80, 0x00, revision };
        }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            magnetometer = metawear.GetModule<IMagnetometerBmm150>();
        }

        static readonly byte[] XY_BITMASK = new byte[] { 0x01, 0x04, 0x07, 0x17 },
            Z_BITMASK = new byte[] { 0x02, 0x0e, 0x1a, 0x52 },
            ODR_BITMASK = new byte[] { 0, 0, 0, 5 };

        [Test, TestCaseSource(typeof(MagnetometerBmm150TestDataClass), "PresetTestCases")]
        public void Configure(Preset preset) {
            byte[][] expected = revision >= 2 ? new byte[][] {
                new byte[] { 0x15, 0x01, 0x00 },
                new byte[] { 0x15, 0x04, XY_BITMASK[(int)preset], Z_BITMASK[(int)preset] },
                new byte[] { 0x15, 0x03, ODR_BITMASK[(int)preset] }
            } : new byte[][] { 
                new byte[] { 0x15, 0x04, XY_BITMASK[(int) preset], Z_BITMASK[(int) preset] },
                new byte[] { 0x15, 0x03, ODR_BITMASK[(int) preset] }
            };

            magnetometer.Configure(preset);
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task CreateAndRemoveStreamAsync() {
            byte[][] expected = new byte[][] {
                new byte[] { 0x15, 0x05, 0x01 },
                new byte[] { 0x15, 0x02, 0x01, 0x00 },
                new byte[] { 0x15, 0x01, 0x01 },
                new byte[] { 0x15, 0x01, 0x00 },
                new byte[] { 0x15, 0x02, 0x00, 0x01 },
                new byte[] { 0x15, 0x05, 0x00 }
            };

            var route = await magnetometer.MagneticField.AddRouteAsync(source => source.Stream(null));
            magnetometer.MagneticField.Start();
            magnetometer.Start();

            magnetometer.Stop();
            magnetometer.MagneticField.Stop();
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task InterpretDataAsync() {
            MagneticField expected = new MagneticField(
                BitConverter.ToSingle(new byte[] { 0x6d, 0xa9, 0x83, 0xb9 }, 0),
                BitConverter.ToSingle(new byte[] { 0x2f, 0x36, 0x2d, 0x39 }, 0),
                BitConverter.ToSingle(new byte[] { 0x9b, 0x8d, 0x95, 0x38 }, 0)
            ), actual = null;

            await magnetometer.MagneticField.AddRouteAsync(source => source.Stream(data => actual = data.Value<MagneticField>()));
            platform.sendMockResponse(new byte[] { 0x15, 0x05, 0x4e, 0xf0, 0x53, 0x0a, 0x75, 0x04 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleSingleAxisDataAsync() {
            float[] expected = new float[] { -0.0002511250f, 0.0001651875f, 0.0000713125f };
            float[] actual = new float[3];

            await magnetometer.MagneticField.AddRouteAsync(source =>
                source.Split().Index(0).Stream(data => actual[0] = data.Value<float>())
                        .Index(1).Stream(data => actual[1] = data.Value<float>())
                        .Index(2).Stream(data => actual[2] = data.Value<float>())
            );

            platform.sendMockResponse(new byte[] { 0x15, 0x05, 0x4e, 0xf0, 0x53, 0x0a, 0x75, 0x04 });
            Assert.That(actual, Is.EqualTo(expected).Within(0.0000003f));
        }

        [Test]
        public async Task HandlePackedResponseAsync() {
            byte[] response = new byte[] {0x15, 0x09, 0xb6, 0x0c, 0x72, 0xf7, 0x89, 0xee, 0xb6,
                0x0b, 0x5a, 0xf8, 0x32, 0xee, 0xe6, 0x0a, 0xa2, 0xf7, 0x25, 0xef};
            MagneticField[] expected = new MagneticField[] {
                new MagneticField(
                    BitConverter.ToSingle(new byte[] { 0x10, 0x41, 0x55, 0x39 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x1a, 0x86, 0x0f, 0xb9 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x77, 0x81, 0x92, 0xb9 }, 0)
                ),
                new MagneticField(
                    BitConverter.ToSingle(new byte[] { 0x18, 0x7a, 0x44, 0x39 }, 0),
                    BitConverter.ToSingle(new byte[] { 0xca, 0x51, 0x00, 0xb9 }, 0),
                    BitConverter.ToSingle(new byte[] { 0x46, 0x5b, 0x95, 0xb9 }, 0)
                ),
                new MagneticField(
                    BitConverter.ToSingle(new byte[] { 0x6f, 0xd8, 0x36, 0x39 }, 0),
                    BitConverter.ToSingle(new byte[] { 0xcc, 0x60, 0x0c, 0xb9 }, 0),
                    BitConverter.ToSingle(new byte[] { 0xd8, 0x64, 0x8d, 0xb9 }, 0)
                )
            }, actual = new MagneticField[3];

            int i = 0;
            await magnetometer.PackedMagneticField.AddRouteAsync(source => source.Stream(data => actual[i++] = data.Value<MagneticField>()));

            platform.sendMockResponse(response);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Suspend() {
            magnetometer.Suspend();

            var expected = revision >= 2 ? Is.EqualTo(new byte[][] { new byte[] { 0x15, 0x01, 0x02 } }) as Constraint : Is.Empty as Constraint;
            Assert.That(platform.GetCommands(), expected);
        }
    }
}
