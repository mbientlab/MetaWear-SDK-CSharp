using MbientLab.MetaWear;
using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Core.DataProcessor;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class LedController {
        internal static async Task Setup(IMetaWearBoard metawear) {
            var mwSwitch = metawear.GetModule<ISwitch>();
            await mwSwitch.State.AddRouteAsync(source => {
                var led = metawear.GetModule<ILed>();
                var count = source.Filter(Comparison.Eq, 1).Count().Name("press-count");

                var colors = Enum.GetValues(typeof(Color)).Cast<Color>();
                count.Filter(Comparison.Eq, colors.Count() + 1).React(token => {
                    led.Stop(true);
                    metawear.GetModule<IDataProcessor>().Edit<ICounterEditor>("press-count").Reset();
                });
                foreach (var c in colors) {
                    count.Filter(Comparison.Eq, ((int) c) + 1).React(token => {
                        led.Stop(true);
                        led.EditPattern(c, Pattern.Solid);
                        led.Play();
                    });
                }
            });
        }

        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            Console.WriteLine($"Configuring {args[0]}...");
            await Setup(metawear);

            Console.WriteLine("Press [Enter] to reset the board");
            Console.ReadLine();

            Console.WriteLine("Resetting device...");
            await metawear.GetModule<IDebug>().ResetAsync();
        }
    }
}