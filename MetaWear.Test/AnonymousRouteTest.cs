using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Impl;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Sensor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Test {
    class AnonymousRouteTest {
        class TestBase : UnitTestBase {
            protected IList<IAnonymousRoute> loggers;

            public TestBase() : base(typeof(IAccelerometerBmi160), typeof(IGyroBmi160), typeof(IMagnetometerBmm150), typeof(ISensorFusionBosch), 
                    typeof(ILogging), typeof(IDataProcessor), typeof(IGpio)) {
            }

            [SetUp]
            public async override Task SetUp() {
                await base.SetUp();

                AddCustomResponses();

                loggers = await metawear.CreateAnonymousRoutesAsync();
            }

            protected virtual void AddCustomResponses() {
                platform.customResponses.Add(new byte[] { 0x3, 0x83 },
                        new byte[] { 0x03, 0x83, 40, 8 });
                platform.customResponses.Add(new byte[] { 0x13, 0x83 },
                        new byte[] { 0x13, 0x83, 40, 3 });
                platform.customResponses.Add(new byte[] { 0x19, 0x82 },
                        new byte[] { 0x19, 0x82, 0x1, 0xf });
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestCreateAndSync : TestBase {
            public TestCreateAndSync() : base() {
            }

            [SetUp]
            public async override Task SetUp() {
                metawear = new MetaWearBoard(platform, platform);
                await metawear.InitializeAsync();

                AddCustomResponses();
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x03, 0x04, 0xff, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82, 0x03, 0x04, 0xff, 0x24 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x24 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
            }

            [Test]
            public async Task SyncLoggers() {
                await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log());
                await metawear.GetModule<IGyroBmi160>().AngularVelocity.AddRouteAsync(source => source.Log());

                loggers = await metawear.CreateAnonymousRoutesAsync();

                Assert.That(loggers.Count, Is.EqualTo(2));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestAcceleration : TestBase {
            public TestAcceleration() : base() {
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x03, 0x04, 0xff, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82, 0x03, 0x04, 0xff, 0x24 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
            }

            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(1));
            }

            [Test]
            public void HandleDownload() {
                Acceleration actual = null;
                Acceleration expected = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x75, 0x3d }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0x80, 0x9e, 0x3d }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0xd0, 0x7d, 0x3f }, 0));

                loggers.First().Subscribe(data => actual = data.Value<Acceleration>());
                platform.sendMockResponse(new sbyte[] { 11, 7, -96, -26, 66, 0, 0, -11, 0, 61, 1, -95, -26, 66, 0, 0, -35, 15, 0, 0 });

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestGyroY : TestBase {
            public TestGyroY() : base() {
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x22 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
            }


            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(1));
            }

            [Test]
            public void HandleDownload() {
                List<float> expected = new List<float>(new float[] { -0.053f, -0.015f });
                List<float> actual = new List<float>();
                loggers[0].Subscribe(data => actual.Add(data.Value<float>()));

                platform.sendMockResponse(new byte[] { 11, 7, 64, 34, 223, 4, 0, 249, 255, 0, 0, 64, 61, 223, 4, 0, 254, 255, 0, 0 });

                Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
            }

            [Test]
            public void CheckIdentifier() {
                Assert.That(loggers[0].Identifier, Is.EqualTo("angular-velocity[1]"));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestSplitImu : TestBase {
            public TestSplitImu() : base() {
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x03, 0x04, 0xff, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82, 0x03, 0x04, 0xff, 0x24 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x24 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
            }

            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(2));
            }

            [Test]
            public void HandleDownload() {
                AngularVelocity actual = null;
                AngularVelocity expected = new AngularVelocity(
                    BitConverter.ToSingle(new byte[] { 0x2c, 0x31, 0x60, 0x43 }, 0), 
                    BitConverter.ToSingle(new byte[] { 0xbc, 0xd2, 0x2a, 0x43 }, 0), 
                    BitConverter.ToSingle(new byte[] { 0x1f, 0x03, 0x13, 0x43 }, 0));
                loggers[1].Subscribe(data => actual = data.Value<AngularVelocity>());

                Acceleration actualAcc = null;
                Acceleration expectedAcc = new Acceleration(
                    BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x75, 0x3d }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0x80, 0x9e, 0x3d }, 0),
                    BitConverter.ToSingle(new byte[] { 0x00, 0xd0, 0x7d, 0x3f }, 0));
                loggers[0].Subscribe(data => actualAcc = data.Value<Acceleration>());

                platform.sendMockResponse(new sbyte[] { 11, 7, 0x60, -26, 66, 0, 0, -11, 0, 61, 1, 0x62, -26, 66, 0, 0, -35, 15, 0, 0 });
                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0x61, 0x38, 0xc2, 0x01, 0x00, 0xe6, 0x72, 0x8c, 0x57, 0x63, 0x38, 0xc2, 0x01, 0x00, 0x58, 0x4b, 0x00, 0x00 });

                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(actualAcc, Is.EqualTo(expectedAcc));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestMultipleLoggers : TestBase {
            public TestMultipleLoggers() : base() {
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x24 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82, 0x13, 0x05, 0xff, 0x22 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
            }

            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(2));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestActivity : TestBase {
            public TestActivity() : base() {   
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x09, 0x03, 0x02, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82, 0x09, 0xc4, 0x03, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0x9, 0x82, 0x00 },
                        new byte[] { 0x09, 0x82, 0x03, 0x04, 0xff, 0xa0, 0x07, 0xa5, 0x00, 0x00, 0x00, 0x00, 0xd0, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                platform.customResponses.Add(new byte[] { 0x9, 0x82, 0x01 },
                        new byte[] { 0x09, 0x82, 0x09, 0x03, 0x00, 0x20, 0x02, 0x07, 0x00, 0x00, 0x00, 0x00, 0xd0, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                platform.customResponses.Add(new byte[] { 0x9, 0x82, 0x02 },
                        new byte[] { 0x09, 0x82, 0x09, 0x03, 0x01, 0x60, 0x08, 0x13, 0x30, 0x75, 0x00, 0x00, 0xd0, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                platform.customResponses.Add(new byte[] { 0x9, 0x82, 0x03 },
                        new byte[] { 0x09, 0x82, 0x09, 0x03, 0x01, 0x60, 0x0f, 0x03, 0x00, 0x00, 0x00, 0x00, 0xd0, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            }

            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(2));
            }

            [Test]
            public void CheckIdentifier() {
                Assert.That(loggers[0].Identifier, Is.EqualTo("acceleration:rms?id=0:accumulate?id=1:time?id=2"));
                Assert.That(loggers[1].Identifier, Is.EqualTo("acceleration:rms?id=0:accumulate?id=1:buffer-state?id=3"));
            }

            [Test]
            public void HandleDownload() {
                List<float> expected = new List<float>(new float[] { 1.16088868f, 1793.6878f, 3545.5054f }),
                    actual = new List<float>();

                loggers[0].Subscribe(data => actual.Add(data.Value<float>()));

                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0x00, 0x3c, 0xe2, 0x01, 0x00, 0x93, 0x12, 0x00, 0x00, 0x00, 0x48, 0x32, 0x02, 0x00, 0x01, 0x1b, 0x70, 0x00 });
                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0x00, 0x53, 0x82, 0x02, 0x00, 0x16, 0x98, 0xdd, 0x00 });

                Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
            }

            [Test]
            public void HandleStateDownload() {
                float expected = 3521.868f, actual = 0f;

                loggers[1].Subscribe(data => actual = data.Value<float>());

                platform.sendMockResponse(new byte[] { 0x0b, 0x07, 0xc1, 0xe9, 0x06, 0x02, 0x00, 0xe3, 0x1d, 0xdc, 0x00 });

                Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestQuaternionLimiter : TestBase {
            public TestQuaternionLimiter() : base() {      
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x09, 0x03, 0x00, 0x60 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82, 0x09, 0x03, 0x00, 0x64 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82, 0x09, 0x03, 0x00, 0x68 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82, 0x09, 0x03, 0x00, 0x6c });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0x9, 0x82, 0x00 },
                        new byte[] { 0x09, 0x82, 0x19, 0x07, 0xff, 0xe0, 0x08, 0x17, 0x14, 0x00, 0x00, 0x00, 0xd0, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            }

            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(1));
            }

            [Test]
            public void CheckIdentifier() {
                Assert.That(loggers[0].Identifier, Is.EqualTo("quaternion:time?id=0"));
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestTimeout : UnitTestBase {
            public TestTimeout() : base(typeof(IAccelerometerBmi160), typeof(IGyroBmi160), typeof(IMagnetometerBmm150), typeof(ISensorFusionBosch), typeof(ILogging), typeof(IDataProcessor)) {
            }
            
            [Test]
            public void AccSyncTimeout() {
                Assert.ThrowsAsync<TimeoutException>(async () => {
                    await metawear.CreateAnonymousRoutesAsync();
                });
            }

            [Test]
            public void GyroSyncTimeout() {
                platform.customResponses[new byte[] { 0x3, 0x83 }] = new byte[] { 0x03, 0x83, 40, 8 };
                
                Assert.ThrowsAsync<TimeoutException>(async () => {
                    await metawear.CreateAnonymousRoutesAsync();
                });
            }

            [Test]
            public void LogTimeout() {
                platform.customResponses[new byte[] { 0x3, 0x83 }] = new byte[] { 0x03, 0x83, 40, 8 };
                platform.customResponses[new byte[] { 0x13, 0x83 }] = new byte[] { 0x13, 0x83, 40, 3 };

                Assert.ThrowsAsync<TimeoutException>(async () => {
                    await metawear.CreateAnonymousRoutesAsync();
                });
            }

            [Test]
            public void DataProcessorTimeout() {
                platform.customResponses[new byte[] { 0x3, 0x83 }] = new byte[] { 0x03, 0x83, 40, 8 };
                platform.customResponses[new byte[] { 0x13, 0x83 }] = new byte[] { 0x13, 0x83, 40, 3 };
                platform.customResponses[new byte[] { 0xb, 0x82, 0x00 }] = new byte[] { 0x0b, 0x82, 0x09, 0x03, 0x02, 0x60 };

                Assert.ThrowsAsync<TimeoutException>(async () => {
                    await metawear.CreateAnonymousRoutesAsync();
                });
            }
        }

        [Parallelizable]
        [TestFixture]
        class TestBmi160StepCounter : TestBase {
            public TestBmi160StepCounter() : base() {
            }

            protected override void AddCustomResponses() {
                base.AddCustomResponses();

                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x00 },
                        new byte[] { 0x0b, 0x82, 0x03, 0xda, 0xff, 0x20 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x01 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x02 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x03 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x04 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x05 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x06 },
                        new byte[] { 0x0b, 0x82 });
                platform.customResponses.Add(new byte[] { 0xb, 0x82, 0x07 },
                        new byte[] { 0x0b, 0x82 });
            }

            [Test]
            public void SyncLoggers() {
                Assert.That(loggers.Count, Is.EqualTo(1));
            }

            [Test]
            public void CheckIdentifier() {
                Assert.That(loggers[0].Identifier, Is.EqualTo("step-counter"));
            }
        }
    }
}
