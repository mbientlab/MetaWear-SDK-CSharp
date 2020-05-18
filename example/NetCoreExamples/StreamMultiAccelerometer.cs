using MbientLab.MetaWear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class StreamMultiAccelerometer {
        static async Task RunAsync(string[] args) {
            var metawears = new List<IMetaWearBoard>();
            var samples = new Dictionary<IMetaWearBoard, uint>();

            // Connect and prepare the BLE connection for every device specified on the command line
            foreach (var _ in args) {
                var m = await StreamAccelerometer.PrepareBleConn(_);

                metawears.Add(m);
                samples.Add(m, 0);

                await StreamAccelerometer.Setup(m, samples);
            }

            foreach (var _ in metawears) {
                StreamAccelerometer.Start(_);
            }

            // Stream for 30s
            await Task.Delay(30000);

            await Task.WhenAll(metawears.Select(_ => StreamAccelerometer.Stop(_)));

            foreach (var (k, v) in samples) {
                Console.WriteLine($"{k.MacAddress} -> {v}");
            }
        }
    }
}