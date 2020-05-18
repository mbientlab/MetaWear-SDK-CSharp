using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class StreamAccelerometer {
        internal static async Task<IMetaWearBoard> PrepareBleConn(string mac) {
            var m = await ScanConnect.Connect(mac);

            // Adjust the max connection interval to support the 100Hz stream
            m.GetModule<ISettings>()?.EditBleConnParams(maxConnInterval: 7.5f);
            await Task.Delay(1500);

            Console.WriteLine($"Connected to {mac}");
            return m;
        }

        internal static async Task Setup(IMetaWearBoard metawear, Dictionary<IMetaWearBoard, uint> samples) {
            var acc = metawear.GetModule<IAccelerometer>();

            // Set the data rate to 100Hz and data range to +/-16g, or closest valid values
            acc.Configure(odr: 100f, range: 16f);
            // Use data route framework to tell the MetaMotion to stream accelerometer data to the host device
            // https://mbientlab.com/csdocs/latest/data_route.html#stream
            await acc.Acceleration.AddRouteAsync(source => source.Stream(data => {
                Console.WriteLine($"{metawear.MacAddress} -> {data.Value<Acceleration>()}");
                samples[metawear]++;
            }));
        }

        internal static void Start(IMetaWearBoard metawear) {
            var acc = metawear.GetModule<IAccelerometer>();
            // Start the acceleration data
            acc.Acceleration.Start();
            // Put accelerometer in active mode
            acc.Start();
        }

        internal static Task Stop(IMetaWearBoard metawear) {
            var acc = metawear.GetModule<IAccelerometer>();

            // Put accelerometer back into standby mode
            acc.Stop();
            // Stop accelerationi data
            acc.Acceleration.Stop();

            // Have remote device close the connection
            return metawear.GetModule<IDebug>().DisconnectAsync();
        }

        static async Task RunAsync(string[] args) {
            var metawear = await PrepareBleConn(args[0]);

            var samples = new Dictionary<IMetaWearBoard, uint> {
                { metawear, 0 }
            };

            // Connect and prepare the BLE connection
            await Setup(metawear, samples);
            Start(metawear);

            // Stream for 30
            await Task.Delay(30000);

            // Stop accelerometer and disconnect from the board
            await Stop(metawear);

            foreach (var (k, v) in samples) {
                Console.WriteLine($"{k.MacAddress} -> {v}");
            }
        }
    }
}