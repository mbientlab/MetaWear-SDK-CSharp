using System.Threading.Tasks;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;

namespace NetCoreExamples {
    class Led {
        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            var led = metawear.GetModule<ILed>();
            // Set a solid pattern for the green led
            led.EditPattern(Color.Green, Pattern.Solid);
            // Play pattern
            led.Play();

            await Task.Delay(5000);
            // Stop and clear pattern
            led.Stop(true);

            // Have remote device disconnect instead to ensure the 
            // LED stop command is received
            await metawear.GetModule<IDebug>().DisconnectAsync();
        }
    }
}