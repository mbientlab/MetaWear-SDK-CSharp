using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.Temperature;
using System;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class StreamTemperature {
        static async Task<IScheduledTask> Setup(IMetaWearBoard metawear) {
            var temperature = metawear.GetModule<ITemperature>();
            var thermistor = temperature.FindSensors(SensorType.PresetThermistor)[0];

            await thermistor.AddRouteAsync(source => source.Stream(_ => {
                Console.WriteLine($"{_.FormattedTimestamp} -> {_.Value<float>()}");
            }));
            // Temperature is classified as a forced data producer  
            // Schedule periodic `Read` calls in the firmware, do it after route is setup
            return await metawear.ScheduleAsync(1000, false, () => thermistor.Read());
        }

        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            Console.WriteLine($"Configuring {args[0]}...");
            var task = await Setup(metawear);
            // Start the periodic task
            task.Start();

            Console.WriteLine("Streaming data for 15s");
            await Task.Delay(15000);

            // Remove scheduled task
            task.Remove();

            await metawear.GetModule<IDebug>().DisconnectAsync();
        }
    }
}
