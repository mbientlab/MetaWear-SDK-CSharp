using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.GyroBmi160;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MbientLab.MetaWear.Test {
    [Parallelizable]
    [TestFixture]
    class LoggingDataTest : UnitTestBase {
        private ILogging logging;

        public LoggingDataTest() : base(typeof(IAccelerometerBmi160), typeof(IGyroBmi160), typeof(ILogging), typeof(IDebug)) { }

        [SetUp]
        public async override Task SetUp() {
            await base.SetUp();

            logging = metawear.GetModule<ILogging>();
        }

        [Test]
        public async Task SetupAndRemoveAccelerationAsync() {
            byte[][] expected = {
                new byte[] {0x0b, 0x02, 0x03, 0x04, 0xff, 0x60},
                new byte[] {0x0b, 0x02, 0x03, 0x04, 0xff, 0x24},
                new byte[] {0x0b, 0x03, 0x00},
                new byte[] {0x0b, 0x03, 0x01}
            };

            var route = await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log());
            platform.fileSuffix = "bmi160_acc_log";
            await metawear.SerializeAsync();
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected));
        }

        [Test]
        public async Task CheckAccelerationDataAsync() {
            List<Acceleration> actual = new List<Acceleration>();
            var route = await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log(data => actual.Add(data.Value<Acceleration>())));

            List<Acceleration> expected = new List<Acceleration>();
            platform.ReadFile("bmi160_expected_values", line => {
                int i = 0;
                float[] value = new float[3];
                foreach (var v in JsonConvert.DeserializeObject<int[]>(line)) {
                    value[i++] = BitConverter.ToSingle(BitConverter.GetBytes(v), 0);
                }

                expected.Add(new Acceleration(value[0], value[1], value[2]));
            });

            List<sbyte[]> responses = new List<sbyte[]>();
            platform.ReadFile("bmi160_log_dl", line => responses.Add(JsonConvert.DeserializeObject<sbyte[]>(line)));

            metawear.GetModule<IAccelerometer>().Configure(range: 8f);
            var task = logging.DownloadAsync();
            responses.ForEach(it => platform.sendMockResponse(it));
            await task;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task CheckAccelerationOffsetsAsync() {
            int[] expected = null;
            platform.ReadFile("bmi160_expected_offsets", line => expected = JsonConvert.DeserializeObject<int[]>(line));

            int[] actual = new int[expected.Length];
            DateTime? prev = null;
            int i = 0;
            var route = await metawear.GetModule<IAccelerometer>().Acceleration.AddRouteAsync(source => source.Log(data => {
                if (prev != null) {
                    actual[i++] = Convert.ToInt32((data.Timestamp - prev.Value).TotalMilliseconds);
                }
                prev = data.Timestamp;
            }));
            
            List<sbyte[]> responses = new List<sbyte[]>();
            platform.ReadFile("bmi160_log_dl", line => responses.Add(JsonConvert.DeserializeObject<sbyte[]>(line)));

            metawear.GetModule<IAccelerometer>().Configure(range: 8f);
            var task = logging.DownloadAsync();
            responses.ForEach(it => platform.sendMockResponse(it));
            await task;

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task CheckGyroYDataAsync() {
            string buffer = "";
            platform.ReadFile("bmi160_gyro_yaxis_expected_values", line => buffer+= line);
            float[] expected = JsonConvert.DeserializeObject<float[]>(buffer);

            float[] actual = new float[expected.Length];
            int i = 0;

            var gyro = metawear.GetModule<IGyroBmi160>();
            gyro.Configure(range: DataRange._250dps);
            var route = await gyro.AngularVelocity.AddRouteAsync(source => source.Split().Index(1).Log(data => actual[i++] = data.Value<float>()));

            logging.DownloadAsync();
            platform.ReadFile("bmi160_gyro_yaxis_dl", line => platform.sendMockResponse(JsonConvert.DeserializeObject<byte[]>(line)));

            Assert.That(actual, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public async Task SetupAndRemoveGyroYAsync() {
            byte[][] expected = {
                new byte[] {0x0b, 0x02, 0x13, 0x05, 0xff, 0x22},
                new byte[] {0x0b, 0x03, 0x00}                
            };

            var gyro = metawear.GetModule<IGyroBmi160>();
            var route = await gyro.AngularVelocity.AddRouteAsync(source => source.Split().Index(1).Log());
            platform.fileSuffix = "bmi160_gyro_y_log";
            await metawear.SerializeAsync();
            route.Remove();

            Assert.That(platform.GetCommands(), Is.EqualTo(expected).Within(0.001f));
        }
    }
}
