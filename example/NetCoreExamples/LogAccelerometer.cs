using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using System;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class LogAccelerometer {
        static async Task SetupLogger(IMetaWearBoard metawear) {
            var acc = metawear.GetModule<IAccelerometer>();
            await acc.Acceleration.AddRouteAsync(source => source.Log(_ => {
                Console.WriteLine($"{_.FormattedTimestamp} -> {_.Value<Acceleration>()}");
            }));

            // Tell firmware to start logging
            metawear.GetModule<ILogging>().Start();
            acc.Acceleration.Start();
            acc.Start();
        }

        static async Task DownloadData(IMetaWearBoard metawear) {
            var logging = metawear.GetModule<ILogging>();
            var acc = metawear.GetModule<IAccelerometer>();

            acc.Stop();
            acc.Acceleration.Stop();
            logging.Stop();

            metawear.GetModule<ISettings>().EditBleConnParams(maxConnInterval: 7.5f);
            await Task.Delay(1500);
            await logging.DownloadAsync();
        }

        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            Console.WriteLine($"Configuring {metawear.MacAddress}...");
            await SetupLogger(metawear);

            Console.WriteLine("Logging data for 15s");
            await Task.Delay(15000);

            Console.WriteLine("Downloading data");
            await DownloadData(metawear);

            await metawear.GetModule<IDebug>().ResetAsync();
        }
    }
}
