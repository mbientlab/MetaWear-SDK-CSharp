using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Builder;

using NUnit.Framework;
using MbientLab.MetaWear.Peripheral;
using System.Threading.Tasks;
using System;
using MbientLab.MetaWear.Core.DataProcessor;
using MbientLab.MetaWear.Peripheral.Led;
using MbientLab.MetaWear.Sensor.Temperature;
using MbientLab.MetaWear.Data;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Test {
    abstract class DataProcessorTest : UnitTestBase {
        public DataProcessorTest() : base(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160), typeof(IGyroBmi160),
            typeof(IBarometerBmp280), typeof(IGpio), typeof(ILogging), typeof(IDataProcessor), typeof(ITemperature)) { }
    }

    [Parallelizable]
    [TestFixture]
    class TestProcessorState : DataProcessorTest {
        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            var accelerometer = metawear.GetModule<IAccelerometer>();
            await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Map(Function1.Rms).Accumulate().Multicast()
                    .To().Buffer().Name("rms_buffer")
                    .To().Limit(30000).Stream()
            );

            var bufferState = metawear.GetModule<IDataProcessor>().State("rms_buffer");
            await bufferState.AddRouteAsync(source => source.Stream().Name("buffer_state_stream"));
        }

        [Test]
        public async Task CreateActivityRouteAsync() {
            byte[][] expected = {
                    new byte[] { 0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x07, 0xa5, 0x00 },
                    new byte[] { 0x09, 0x02, 0x09, 0x03, 0x00, 0x20, 0x02, 0x07 },
                    new byte[] { 0x09, 0x02, 0x09, 0x03, 0x01, 0x60, 0x0f, 0x03 },
                    new byte[] { 0x09, 0x02, 0x09, 0x03, 0x01, 0x60, 0x08, 0x13, 0x30, 0x75, 0x00, 0x00 },
                    new byte[] { 0x09, 0x03, 0x01 },
                    new byte[] { 0x09, 0x07, 0x03, 0x01 }
            };

            platform.fileSuffix = "activity_buffer";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void HandleBufferStateData() {
            float expected = 260.5125f;
            float actual = 0f;

            metawear.LookupRoute(1).LookupSubscriber("buffer_state_stream").Attach(data => actual = data.Value<float>());

            platform.sendMockResponse(new byte[] { 0x09, 0x84, 0x02, 0xcd, 0x20, 0x41, 0x00 });
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestRms : DataProcessorTest {
        [Test]
        public async Task SetAccSumAsync() {
            byte[] expected = new byte[] { 0x09, 0x04, 0x01, 0x00, 0x00, 0x71, 0x02 };

            var accelerometer = metawear.GetModule<IAccelerometer>();
            accelerometer.Configure(range: 16f, odr: 100f);

            await accelerometer.Acceleration.AddRouteAsync(source => source.Map(Function1.Rms).Accumulate().Name("rms_acc"));
            metawear.GetModule<IDataProcessor>().Edit<IAccumulatorEditor>("rms_acc").Set(20000f);

            Assert.That(platform.GetLastCommand(), Is.EqualTo(expected));
        }

        [Test]
        public async Task CreateRmsLoggerAsync() {
            byte[][] expected = new byte[][] {
                    new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x07, 0xa5, 0x00},
                    new byte[] {0x0b, 0x02, 0x09, 0x03, 0x00, 0x20}
            };

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Map(Function1.Rms).Log());

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestAccRightShift : DataProcessorTest {
        [Test]
        public async Task CreateLog() {
            byte[][] expected = new byte[][] {
                    new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x09, 0x14, 0x08, 0x08, 0x00, 0x00, 0x00, 0x02},
                    new byte[] {0x0b, 0x02, 0x09, 0x03, 0x00, 0x40}
            };

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Map(Function2.RightShift, 8).Log());
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleData() {
            float[] expected = new float[] { 1.969f, 0.812f, 0.984f };
            float[] actual = new float[3];

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source =>
                source.Map(Function2.RightShift, 8).Stream(data => {
                    byte[] bytes = data.Bytes;
                    for (int i = 0; i < bytes.Length; i++) {
                        actual[i] = (bytes[i] << 8) / data.Scale;
                    }
                })
            );

            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x00, 126, 52, 63 });
            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestLedController : DataProcessorTest {
        [Test]
        public async Task Create() {
            byte[][] expected = new byte[][] {
                new byte[] {0x09, 0x02, 0x01, 0x01, 0xff, 0x00, 0x02, 0x13},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x00, 0x60, 0x09, 0x0f, 0x04, 0x02, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x60, 0x06, 0b00000110, 0x01, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x60, 0x06, 0b00000110, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x02, 0x02, 0x03, 0x0f},
                new byte[] {0x0a, 0x03, 0x02, 0x02, 0x10, 0x10, 0x00, 0x00, 0xf4, 0x01, 0x00, 0x00, 0xe8, 0x03, 0x00, 0x00, 0xff},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x02, 0x02, 0x01, 0x01},
                new byte[] {0x0a, 0x03, 0x01},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x03, 0x02, 0x02, 0x01},
                new byte[] {0x0a, 0x03, 0x01}
            };

            ILed led = metawear.GetModule<ILed>();
            await metawear.GetModule<ISwitch>().State.AddRouteAsync(source => {
                source.Count().Map(Function2.Modulus, 2).Multicast()
                        .To().Filter(Comparison.Eq, ComparisonOutput.Absolute, 1).React(token => {
                            led.EditPattern(Color.Blue, high: 16, low: 16, duration: 1000, highTime: 500);
                            led.Play();
                        })
                        .To().Filter(Comparison.Eq, ComparisonOutput.Absolute, 0).React(token => led.Stop(true));
            });

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestFreeFall : DataProcessorTest {
        [SetUp]
        public async override Task SetUp() {
            platform.initResponse.moduleResponses[0x9][3] = 0x1;
            await base.SetUp();
        }

        [Test]
        public async Task Create() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x07, 0xa5, 0x01},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x00, 0x20, 0x03, 0x05, 0x04},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x20, 0x0d, 0x09, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x02, 0x00, 0x06, 0b00000001, 0xff},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x02, 0x00, 0x06, 0b00000001, 0x01}
            };

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source =>
                source.Map(Function1.Rss).LowPass(4).Find(Threshold.Binary, 0.5f)
                    .Multicast()
                        .To().Filter(Comparison.Eq, -1)
                        .To().Filter(Comparison.Eq, 1)
            );

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestFeedback : DataProcessorTest {
        [Test]
        public async Task ComparatorFeedbackAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x05, 0xc7, 0x00, 0x20, 0x06, 0x22, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x00, 0x09, 0x05, 0x05, 0x05, 0x03},
                new byte[] {0x0a, 0x03, 0x00, 0x06, 0x22, 0x00, 0x00}
            };

            await metawear.GetModule<IGpio>().Pins[0].Adc.AddRouteAsync(source => source.Filter(Comparison.Gt, "reference").Name("reference"));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task GpioFeedbackAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x05, 0xc6, 0x00, 0x20, 0x01, 0x02, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x05, 0xc6, 0x00, 0x20, 0x09, 0x05, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x20, 0x06, 0b00100011, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x02, 0x20, 0x02, 0x17},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x03, 0x60, 0x06, 0b00000110, 0x10, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x20, 0x06, 0b00011011, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x05, 0x20, 0x02, 0x17},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x06, 0x60, 0x06, 0b00000110, 0x10, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x00, 0x09, 0x05, 0x09, 0x05, 0x04},
                new byte[] {0x0a, 0x03, 0x01, 0x09, 0x05, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x00, 0x09, 0x04, 0x05},
                new byte[] {0x0a, 0x03, 0x06, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x00, 0x09, 0x04, 0x05},
                new byte[] {0x0a, 0x03, 0x03, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x02, 0x09, 0x04, 0x05},
                new byte[] {0x0a, 0x03, 0x06, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x04, 0x09, 0x04, 0x03},
                new byte[] {0x0a, 0x03, 0x00, 0x01, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x05, 0x09, 0x04, 0x05},
                new byte[] {0x0a, 0x03, 0x03, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x0a, 0x02, 0x09, 0x03, 0x07, 0x09, 0x04, 0x03},
                new byte[] {0x0a, 0x03, 0x00, 0x01, 0x00}
            };

            var dataprocessor = metawear.GetModule<IDataProcessor>();

            await metawear.GetModule<IGpio>().Pins[0].AbsoluteReference.AddRouteAsync(source => {
                source.Multicast()
                    .To().Limit(Passthrough.Count, 0).Name("adc").React(token => {
                        dataprocessor.Edit<ICounterEditor>("lte_count").Reset();
                        dataprocessor.Edit<ICounterEditor>("gt_count").Reset();
                    })
                    .To().Map(Function2.Subtract, "adc").Multicast()
                        .To().Filter(Comparison.Gt, 0f).React(token => dataprocessor.Edit<ICounterEditor>("lte_count").Reset())
                            .Count().Name("gt_count")
                            .Filter(Comparison.Eq, 16f).React(token =>
                                dataprocessor.Edit<IPassthroughEditor>("adc").Set(1))
                        .To().Filter(Comparison.Lte, 0f).React(token => dataprocessor.Edit<ICounterEditor>("gt_count").Reset())
                            .Count().Name("lte_count")
                            .Filter(Comparison.Eq, 16f).React(token =>
                                dataprocessor.Edit<IPassthroughEditor>("adc").Set(1));
            });
            platform.fileSuffix = "gpio_feedback";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void MissingFeedbackName() {
            Assert.ThrowsAsync<IllegalRouteOperationException>(async () => {
                try {
                    await metawear.GetModule<IGpio>().Pins[0].Adc.AddRouteAsync(source => source.Map(Function2.Add, "non-existant"));
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestPulse : DataProcessorTest {
        [Test]
        public async Task AdcPulseAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x05, 0xc7, 0x00, 0x20, 0x0b, 0x01, 0x00, 0x01, 0x00, 0x02, 0x00, 0x00, 0x10, 0x00},
                new byte[] {0x09, 0x03, 0x01},
                new byte[] {0x09, 0x07, 0x00, 0x01}
            };

            await metawear.GetModule<IGpio>().Pins[0].Adc.AddRouteAsync(source => source.Find(Pulse.Area, 512, 16).Stream());
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestMaths : DataProcessorTest {
        [Test]
        public async Task TemperatureConvertor() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x04, 0x81, 0x00, 0x20, 0x09, 0x17, 0x02, 0x12, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x00, 0x60, 0x09, 0x1f, 0x03, 0x0a, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x60, 0x09, 0x1f, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x04, 0x81, 0x00, 0x20, 0x09, 0x17, 0x01, 0x89, 0x08, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x03, 0x01},
                new byte[] {0x09, 0x07, 0x02, 0x01},
                new byte[] {0x09, 0x03, 0x01},
                new byte[] {0x09, 0x07, 0x03, 0x01},
                new byte[] {0x04, 0x81, 0x00}
            };

            var sensor = metawear.GetModule<ITemperature>().FindSensors(SensorType.NrfSoc)[0];
            await sensor.AddRouteAsync(source =>
                source.Multicast()
                    .To().Stream(null)
                    .To().Map(Function2.Multiply, 18).Map(Function2.Divide, 10).Map(Function2.Add, 32).Stream()
                    .To().Map(Function2.Add, 273.15f).Stream(null)
            );
            sensor.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestComparator : DataProcessorTest {
        [Test]
        public async Task GpioMultiCompAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x05, 0xc7, 0x15, 0x20, 0x06, 0x2a, 0x00, 0x04, 0x00, 0x02, 0x00, 0x01, 0x80, 0x00 }
            };

            var pin = metawear.GetModule<IGpio>().Pins[0].CreateVirtualPin(0x15);
            await pin.Adc.AddRouteAsync(source => source.Filter(Comparison.Gte, ComparisonOutput.Absolute, 1024, 512, 256, 128).Name("multi_comp"));

            platform.fileSuffix = "multi_comparator";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestPacker : DataProcessorTest {
        [Test]
        public async Task CreateTempPackerAsync() {
            byte[][] expected = {
                new byte[] { 0x9, 0x2, 0x4, 0xc1, 0x1, 0x20, 0x10, 0x1, 0x3 }
            };
            var sensor = metawear.GetModule<ITemperature>().FindSensors(SensorType.PresetThermistor)[0];

            await sensor.AddRouteAsync(source => source.Pack(4));

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleData() {
            float[] expected = new float[] { 30.625f, 30.125f, 30.25f, 30.25f };
            var sensor = metawear.GetModule<ITemperature>().FindSensors(SensorType.PresetThermistor)[0];

            int i = 0;
            float[] actual = new float[expected.Length];
            await sensor.AddRouteAsync(source => source.Pack(4).Stream(data => actual[i++] = data.Value<float>()));
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x00, 0xf5, 0x00, 0xf1, 0x00, 0xf2, 0x00, 0xf2, 0x00 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void PacketOverflow() {
            Assert.ThrowsAsync<IllegalRouteOperationException>(async () => {
                try {
                    await metawear.GetModule<IBarometerBosch>().Pressure.AddRouteAsync(
                        source => source.Pack(5)
                    );
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestAccounter : DataProcessorTest {
        [Test]
        public async Task CreateTempAccounterAsync() {
            byte[][] expected = {
                new byte[] {0x9, 0x2, 0x3, 0x4, 0xff, 0xa0, 0x11, 0x31, 0x3}
            };

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Account());

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task ExtractDataAsync() {
            Acceleration expected = new Acceleration(
                BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x41, 0x3c }, 0),
                BitConverter.ToSingle(new byte[] { 0x00, 0xc4, 0x12, 0x3f }, 0),
                BitConverter.ToSingle(new byte[] { 0x00, 0x9c, 0x4b, 0xbf }, 0)
            ), actual = null;

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Account().Stream(data => actual = data.Value<Acceleration>()));
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x00, 0xa6, 0x33, 0x0d, 0x00, 0xc1, 0x00, 0xb1, 0x24, 0x19, 0xcd });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ExtractTimeAsync() {
            int[] expected = new int[] { 10, 10, 9, 10, 11 };
            int[] actual = new int[expected.Length];

            int i = 0;
            DateTime? prev = null;
            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Account().Stream(data => {
                if (prev != null) {
                    actual[i++] = Convert.ToInt32((data.Timestamp - prev.Value).TotalMilliseconds);
                }
                prev = data.Timestamp;
            }));

            byte[][] responses = {
                    new byte[] {0x09, 0x03, 0x00, 0xa6, 0x33, 0x0d, 0x00, 0xc1, 0x00, 0xb1, 0x24, 0x19, 0xcd},
                    new byte[] {0x09, 0x03, 0x00, 0xad, 0x33, 0x0d, 0x00, 0xd4, 0x00, 0x18, 0x25, 0xc0, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0xb4, 0x33, 0x0d, 0x00, 0xc7, 0x00, 0x09, 0x25, 0xb2, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0xba, 0x33, 0x0d, 0x00, 0xc5, 0x00, 0x17, 0x25, 0xbc, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0xc1, 0x33, 0x0d, 0x00, 0xd4, 0x00, 0xe9, 0x24, 0xe4, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0xc8, 0x33, 0x0d, 0x00, 0xaf, 0x00, 0xf7, 0x24, 0xe3, 0xcc}
            };
            foreach (byte[] it in responses) {
                platform.sendMockResponse(it);
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleRollbackAsync() {
            int[] expected = new int[] { 11, 10, 9, 10, 10 };
            int[] actual = new int[expected.Length];

            int i = 0;
            DateTime? prev = null;
            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Account().Stream(data => {
                if (prev != null) {
                    actual[i++] = Convert.ToInt32((data.Timestamp - prev.Value).TotalMilliseconds);
                }
                prev = data.Timestamp;
            }));

            byte[][] responses = {
                    new byte[] {0x09, 0x03, 0x00, 0xff, 0xff, 0xff, 0xff, 0xc1, 0x00, 0xb1, 0x24, 0x19, 0xcd},
                    new byte[] {0x09, 0x03, 0x00, 0x06, 0x00, 0x00, 0x00, 0xd4, 0x00, 0x18, 0x25, 0xc0, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0x0d, 0x00, 0x00, 0x00, 0xc7, 0x00, 0x09, 0x25, 0xb2, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0x13, 0x00, 0x00, 0x00, 0xc5, 0x00, 0x17, 0x25, 0xbc, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0x1a, 0x00, 0x00, 0x00, 0xd4, 0x00, 0xe9, 0x24, 0xe4, 0xcc},
                    new byte[] {0x09, 0x03, 0x00, 0x21, 0x00, 0x00, 0x00, 0xaf, 0x00, 0xf7, 0x24, 0xe3, 0xcc}
            };
            foreach (var it in responses) {
                platform.sendMockResponse(it);
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task CreateCountMode() {
            byte[][] expected = {
                new byte[] { 0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x11, 0x30, 0x03 }
            };

            var accelerometer = metawear.GetModule<IAccelerometer>();
            await accelerometer.Acceleration.AddRouteAsync(source => source.Account(AccountType.Count));

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task CountData() {
            uint? count = null;

            var accelerometer = metawear.GetModule<IAccelerometer>();
            await accelerometer.Acceleration.AddRouteAsync(source => source.Account(AccountType.Count).Stream(data => count = data.Extra<uint>()));
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x00, 0xec, 0x01, 0x00, 0x00, 0x01, 0x0b, 0x9a, 0x07, 0x40, 0x40 });

            Assert.That(count.Value, Is.EqualTo(492));
        }

        [Test]
        public async Task CountAndTime() {
            var barometer = metawear.GetModule<IBarometerBosch>();
            var accelerometer = metawear.GetModule<IAccelerometer>();

            DateTime? prev = null;
            List<int> offsets = new List<int>();
            await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Pack(2).Account(AccountType.Count).Stream()
            );
            await barometer.Pressure.AddRouteAsync(source => source.Account().Stream(_ => {
                if (prev.HasValue) {
                    offsets.Add(Convert.ToInt32((_.Timestamp - prev.Value).TotalMilliseconds));
                }
                prev = _.Timestamp;
                Console.WriteLine("time: " + _.Timestamp.ToString("MM/dd/yyyy hh:mm:ss.fffffff"));
            }));

            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x02, 0x72, 0xA4, 0x03, 0x00, 0x77, 0x6C, 0x84, 0x01 });
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x01, 0x8D, 0x00, 0x00, 0x00, 0x4E, 0xFF, 0x35, 0xFD, 0x79, 0x07, 0x4D, 0xFF, 0x35, 0xFD, 0x7D, 0x07 });
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x02, 0xA4, 0xA4, 0x03, 0x00, 0x05, 0x65, 0x84, 0x01 });

            Assert.That(offsets, Is.EqualTo(new List<int>() { 73 }));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestAccounterPackerChain : DataProcessorTest {
        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            var sensor = metawear.GetModule<ITemperature>().FindSensors(SensorType.PresetThermistor)[0];
            await sensor.AddRouteAsync(source => source.Account().Pack(2).Stream());
        }

        [Test]
        public void CreateChain() {
            byte[][] expected = {
                    new byte[] {0x9, 0x2, 0x4, 0xc1, 0x1, 0x20, 0x11, 0x31, 0x3},
                    new byte[] {0x9, 0x2, 0x9, 0x3, 0x0, 0xa0, 0x10, 0x5, 0x1},
                    new byte[] {0x09, 0x03, 0x01},
                    new byte[] {0x09, 0x07, 0x01, 0x01}
            };

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void ExtractTime() {
            int[] expected = new int[] { 33, 33, 33 };
            int[] actual = new int[expected.Length];

            int i = 0;
            DateTime? prev = null;
            metawear.LookupRoute(0).Subscribers[0].Attach(data => {
                if (prev != null) {
                    actual[i++] = Convert.ToInt32((data.Timestamp - prev.Value).TotalMilliseconds);
                }
                prev = data.Timestamp;
            });

            byte[][] responses = {
                    new byte[] {0x0b, 0x84, 0xf5, 0x62, 0x02, 0x00, 0x00},
                    new byte[] {0x09, 0x03, 0x01, 0x7b, 0x64, 0x02, 0x00, 0xec, 0x00, 0x92, 0x64, 0x02, 0x00, 0xeb, 0x00},
                    new byte[] {0x09, 0x03, 0x01, 0xa8, 0x64, 0x02, 0x00, 0xef, 0x00, 0xbf, 0x64, 0x02, 0x00, 0xed, 0x00}
            };
            foreach (byte[] it in responses) {
                platform.sendMockResponse(it);
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractData() {
            float[] expected = new float[] { 29.5f, 29.375f, 29.875f, 29.625f };
            float[] actual = new float[expected.Length];

            int i = 0;
            metawear.LookupRoute(0).Subscribers[0].Attach(data => actual[i++] = data.Value<float>());

            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x01, 0x7b, 0x64, 0x02, 0x00, 0xec, 0x00, 0x92, 0x64, 0x02, 0x00, 0xeb, 0x00 });
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x01, 0xa8, 0x64, 0x02, 0x00, 0xef, 0x00, 0xbf, 0x64, 0x02, 0x00, 0xed, 0x00 });
        }

        [Test]
        public void PacketOverflow() {
            Assert.ThrowsAsync<IllegalRouteOperationException>(async () => {
                try {
                    await metawear.GetModule<IBarometerBosch>().Pressure.AddRouteAsync(
                        source => source.Pack(4).Account()
                    );
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestPackerAccounterChain : DataProcessorTest {
        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            var sensor = metawear.GetModule<ITemperature>().FindSensors(SensorType.PresetThermistor)[0];
            await sensor.AddRouteAsync(source => source.Pack(4).Account().Stream());
        }

        [Test]
        public void CreateChain() {
            byte[][] expected = {
                    new byte[] {0x09, 0x02, 0x04, 0xc1, 0x01, 0x20, 0x10, 0x01, 0x03},
                    new byte[] {0x09, 0x02, 0x09, 0x03, 0x00, 0xe0, 0x11, 0x31, 0x03},
                    new byte[] {0x09, 0x03, 0x01},
                    new byte[] {0x09, 0x07, 0x01, 0x01}
            };

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void ExtractData() {
            float[] expected = new float[] { 24.5f, 24.625f, 24.5f, 24.375f, 24.25f, 24.375f, 24.5f, 24.25f };
            float[] actual = new float[expected.Length];

            int i = 0;
            metawear.LookupRoute(0).Subscribers[0].Attach(data => actual[i++] = data.Value<float>());

            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x01, 0x04, 0x85, 0xa0, 0x00, 0xc4, 0x00, 0xc5, 0x00, 0xc4, 0x00, 0xc3, 0x00 });
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x01, 0x5e, 0x85, 0xa0, 0x00, 0xc2, 0x00, 0xc3, 0x00, 0xc4, 0x00, 0xc2, 0x00 });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractOffset() {
            int[] expected = new int[] { 0, 0, 0, 132, 0, 0, 0, 132, 0, 0, 0, 133, 0, 0, 0 };
            int[] actual = new int[expected.Length];

            int i = 0;
            DateTime? prev = null;
            metawear.LookupRoute(0).Subscribers[0].Attach(data => {
                if (prev != null) {
                    actual[i++] = Convert.ToInt32((data.Timestamp - prev.Value).TotalMilliseconds);
                }
                prev = data.Timestamp;
            });

            byte[][] responses = {
                    new byte[] {0x0b, 0x84, 0x1c, 0x84, 0xa0, 0x00, 0x01},
                    new byte[] {0x09, 0x03, 0x01, 0x04, 0x85, 0xa0, 0x00, 0xc4, 0x00, 0xc5, 0x00, 0xc4, 0x00, 0xc3, 0x00},
                    new byte[] {0x09, 0x03, 0x01, 0x5e, 0x85, 0xa0, 0x00, 0xc2, 0x00, 0xc3, 0x00, 0xc4, 0x00, 0xc2, 0x00},
                    new byte[] {0x09, 0x03, 0x01, 0xb8, 0x85, 0xa0, 0x00, 0xc3, 0x00, 0xc4, 0x00, 0xc3, 0x00, 0xc3, 0x00},
                    new byte[] {0x09, 0x03, 0x01, 0x13, 0x86, 0xa0, 0x00, 0xc5, 0x00, 0xc3, 0x00, 0xc5, 0x00, 0xc2, 0x00},
            };
            foreach (byte[] it in responses) {
                platform.sendMockResponse(it);
            }

        }
    }

    [Parallelizable]
    [TestFixture]
    class TestHighPassFilter : DataProcessorTest {
        [Test]
        public async Task CreateAccHpfAsync() {
            byte[][] expected = {
                    new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x03, 0x25, 0x04, 0x02}
            };
            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.HighPass(4));

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleAccDataAsync() {
            var expected = new Acceleration(
                BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x88, 0xba }, 0),
                BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0x24, 0x3b }, 0),
                BitConverter.ToSingle(new byte[] { 0x00, 0x00, 0xb0, 0x3a }, 0)
            );
            Acceleration actual = null;

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source =>
                source.HighPass(4).Stream(data => actual = data.Value<Acceleration>())
            );
            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x00, 0xef, 0xff, 0x29, 0x00, 0x16, 0x00 });

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestDelay : DataProcessorTest {
        [Test]
        public async Task CreateAccHistory() {
            byte[][] expected = new byte[][] {
                    new byte[] { 0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x0a, 0x05, 0x10 },
                    new byte[] { 0x09, 0x02, 0x09, 0x03, 0x00, 0xa0, 0x01, 0x02, 0x00, 0x00 },
                    new byte[] { 0x0a, 0x02, 0x03, 0x08, 0xff, 0x09, 0x04, 0x03 },
                    new byte[] { 0x0a, 0x03, 0x01, 0x20, 0x00 }
            };

            var accelerometer = metawear.GetModule<IAccelerometerBmi160>();

            await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Delay(16).Limit(Passthrough.Count, 0).Name("history")
            );

            var dataprocessor = metawear.GetModule<IDataProcessor>();
            await accelerometer.LowAndHighG.AddRouteAsync(source =>
                source.React(token => dataprocessor.Edit<IPassthroughEditor>("history").Set(32))
            );

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task SplitOutput() {
            var accelerometer = metawear.GetModule<IAccelerometerBmi160>();

            var route = await accelerometer.Acceleration.AddRouteAsync(source =>
                source.Stream().Delay(8).Split()
                    .Index(0).Accumulate().Stream().Name("split_acc")
            );

            {
                byte[][] expected = new byte[][] {
                    new byte[] { 0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x0a, 0x05, 0x08 },
                    new byte[] { 0x09, 0x02, 0x09, 0x03, 0x00, 0x20, 0x02, 0x07 },
                    new byte[] { 0x03, 0x04, 0x01 },
                    new byte[] { 0x09, 0x03, 0x01 },
                    new byte[] { 0x09, 0x07, 0x01, 0x01 }
                };
                Assert.That(platform.GetCommands(), Is.EqualTo(expected));
            }

            {
                float? actual = null;
                route.LookupSubscriber("split_acc").Attach(data => actual = data.Value<float>());

                platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x01, 0x9f, 0xc9, 0x12, 0x00 });
                Assert.That(actual.Value, Is.EqualTo(75.150f).Within(0.001));
            }
        }
    }

    [Parallelizable]
    [TestFixture]
    class TestFuser : DataProcessorTest {
        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            var acc = metawear.GetModule<IAccelerometer>();
            var gyro = metawear.GetModule<IGyroBmi160>();

            await gyro.AngularVelocity.AddRouteAsync(source => source.Buffer().Name("gyro-buffer"));
            await acc.Acceleration.AddRouteAsync(source => source.Fuse("gyro-buffer").Limit(20).Stream());
        }

        [Test]
        public void CreateAccGyroFusion() {
            byte[][] expected = new byte[][] {
                    new byte[] {0x09, 0x02, 0x13, 0x05, 0xff, 0xa0, 0x0f, 0x05},
                    new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x1b, 0x01, 0x00},
                    new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x60, 0x08, 0x13, 0x14, 0x00, 0x00, 0x00},
                    new byte[] {0x09, 0x03, 0x01},
                    new byte[] {0x09, 0x07, 0x02, 0x01}
            };

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task HandleData() {
            IData accData = null, gyroData = null;
            metawear.LookupRoute(1).Subscribers[0].Attach(_ => {
                var values = _.Value<IData[]>();
                accData = values[0];
                gyroData = values[1];
            });


            IData accRaw = null, gyroRaw = null;
            var acc = metawear.GetModule<IAccelerometer>();
            var gyro = metawear.GetModule<IGyroBmi160>();

            await gyro.AngularVelocity.AddRouteAsync(source => source.Stream(_ => gyroRaw = _));
            await acc.Acceleration.AddRouteAsync(source => source.Stream(_ => accRaw = _));

            platform.sendMockResponse(new byte[] { 0x09, 0x03, 0x02, 0xf4, 0x0d, 0x3c, 0x39, 0x99, 0x11, 0x01, 0x80, 0xd6, 0x91, 0xd3, 0x67 });
            platform.sendMockResponse(new byte[] { 0x03, 0x04, 0xf4, 0x0d, 0x3c, 0x39, 0x99, 0x11 });
            platform.sendMockResponse(new byte[] { 0x13, 0x05, 0x01, 0x80, 0xd6, 0x91, 0xd3, 0x67 });

            Assert.That(accData.Value<Acceleration>(), Is.EqualTo(accRaw.Value<Acceleration>()));
            Assert.That(gyroData.Value<AngularVelocity>(), Is.EqualTo(gyroRaw.Value<AngularVelocity>()));
        }
    }
}
