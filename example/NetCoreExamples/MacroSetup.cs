using MbientLab.MetaWear.Core;
using System;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class MacroSetup {
        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);
            var macro = metawear.GetModule<IMacro>();

            Console.WriteLine($"Configuring {args[0]}...");
            macro.StartRecord();
            await LedController.Setup(metawear);
            await macro.EndRecordAsync();

            Console.WriteLine("Resetting device");
            await metawear.GetModule<IDebug>().ResetAsync();
        }
    }
}