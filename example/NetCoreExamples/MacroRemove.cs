using MbientLab.MetaWear.Core;
using System;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class MacroRemove {
        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            Console.WriteLine("Removing macros");
            metawear.GetModule<IMacro>().EraseAll();

            var debug = metawear.GetModule<IDebug>();
            debug.ResetAfterGc();
            await debug.DisconnectAsync();
        }
    }
}
