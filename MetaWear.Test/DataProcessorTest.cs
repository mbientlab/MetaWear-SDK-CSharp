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

namespace MbientLab.MetaWear.Test {
    [TestFixture]
    class DataProcessorTest : UnitTestBase {
        public DataProcessorTest() : base(typeof(ISwitch), typeof(ILed), typeof(IAccelerometerBmi160), 
            typeof(IGpio), typeof(ILogging), typeof(IDataProcessor), typeof(ITemperature)) { }

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

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Map(Function1.Rms).Log(null));

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task AccRightShiftAsync() {
            byte[][] expected = new byte[][] {
                    new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x09, 0x14, 0x08, 0x08, 0x00, 0x00, 0x00, 0x02},
                    new byte[] {0x0b, 0x02, 0x09, 0x03, 0x00, 0x40}
            };

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Map(Function2.RightShift, 8).Log(null));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task AccRightShiftDataAsync() {
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

        [Test]
        public async Task LedControllerAsync() {
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

        [Test]
        public async Task FreeFallDetectorAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x03, 0x04, 0xff, 0xa0, 0x07, 0xa5, 0x01},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x00, 0x20, 0x03, 0x05, 0x04},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x01, 0x20, 0x0d, 0x09, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x02, 0x00, 0x06, 0b00000001, 0xff},
                new byte[] {0x09, 0x02, 0x09, 0x03, 0x02, 0x00, 0x06, 0b00000001, 0x01}
            };

            await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source =>
                source.Map(Function1.Rss).Average(4).Find(Threshold.Binary, 0.5f)
                    .Multicast()
                        .To().Filter(Comparison.Eq, -1)
                        .To().Filter(Comparison.Eq, 1)
            );

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

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
        public async Task AdcPulseAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x05, 0xc7, 0x00, 0x20, 0x0b, 0x01, 0x00, 0x01, 0x00, 0x02, 0x00, 0x00, 0x10, 0x00},
                new byte[] {0x09, 0x03, 0x01},
                new byte[] {0x09, 0x07, 0x00, 0x01}
            };

            await metawear.GetModule<IGpio>().Pins[0].Adc.AddRouteAsync(source => source.Find(Pulse.Area, 512, 16).Stream(null));
            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

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
                    .To().Map(Function2.Multiply, 18).Map(Function2.Divide, 10).Map(Function2.Add, 32).Stream(null)
                    .To().Map(Function2.Add, 273.15f).Stream(null)
            );
            sensor.Read();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public void MissingFeedbackName() {
            Assert.Throws<IllegalRouteOperationException>(() => {
                try {
                    metawear.GetModule<IGpio>().Pins[0].Adc.AddRouteAsync(source => source.Map(Function2.Add, "non-existant")).Wait();
                } catch (AggregateException e) {
                    throw e.InnerException;
                }
            });
        }

        [Test]
        public async Task GpioMultiCompAsync() {
            byte[][] expected = {
                new byte[] {0x09, 0x02, 0x05, 0xc7, 0x15, 0x20, 0x06, 0x2a, 0x00, 0x04, 0x00, 0x02, 0x00, 0x01, 0x80, 0x00 }
            };

            var pin = metawear.GetModule<IGpio>().CreateVirtualPin(0x15);
            await pin.Adc.AddRouteAsync(source => source.Filter(Comparison.Gte, ComparisonOutput.Absolute, 1024, 512, 256, 128).Name("multi_comp"));

            platform.fileSuffix = "multi_comparator";
            await metawear.SerializeAsync();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }
    }
}
