using MbientLab.MetaWear.Core;
using System;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class AnonymousRoutes {
        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            Console.WriteLine("Creating anonymous routes");
            metawear.GetModule<ISettings>().EditBleConnParams(maxConnInterval: 7.5f);
            await Task.Delay(1500);

            var routes = await metawear.CreateAnonymousRoutesAsync();
            var logging = metawear.GetModule<ILogging>();
            logging.Stop();

            Console.WriteLine($"{routes.Count} active loggers discovered");
            foreach (var r in routes) {
                r.Subscribe(_ =>
                    Console.WriteLine($"identifier: {r.Identifier}, time: {_.FormattedTimestamp}, data: [{BitConverter.ToString(_.Bytes).ToLower().Replace("-", ", 0x")}]")
                );
            }

            await logging.DownloadAsync();

            metawear.GetModule<IMacro>().EraseAll();
            var debug = metawear.GetModule<IDebug>();
            debug.ResetAfterGc();
            await debug.DisconnectAsync();
        }
    }
}