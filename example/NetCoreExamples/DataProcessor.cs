using MbientLab.MetaWear;
using MbientLab.MetaWear.Core;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Sensor.GyroBmi160;
using System;
using System.Threading.Tasks;

namespace NetCoreExamples {
    class DataProcessor {
        static async Task RunAsync(string[] args) {
            var metawear = await ScanConnect.Connect(args[0]);

            Console.WriteLine($"Configuring {args[0]}...");

            var acc = metawear.GetModule<IAccelerometer>();
            var gyro = metawear.GetModule<IGyroBmi160>();

            acc.Configure(odr: 25f);
            gyro.Configure(odr: OutputDataRate._25Hz);

            await gyro.AngularVelocity.AddRouteAsync(source => source.Buffer().Name("gyro"));
            await acc.Acceleration.AddRouteAsync(source => source.Fuse("gyro").Stream(_ => {
                var array = _.Value<IData[]>();

                // accelerometer is the source input, index 0
                // gyro name is first input, index 1
                Console.WriteLine($"acc = {array[0].Value<Acceleration>()}, gyro = {array[1].Value<AngularVelocity>()}");
            }));

            gyro.AngularVelocity.Start();
            acc.Acceleration.Start();

            gyro.Start();
            acc.Start();

            await Task.Delay(15000);

            Console.WriteLine("Resetting device");
            await metawear.GetModule<IDebug>().ResetAsync();
        }
    }
}
