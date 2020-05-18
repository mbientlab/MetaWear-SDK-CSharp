using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MbientLab.Warble;
using MbientLab.MetaWear;
using MbientLab.MetaWear.NetStandard;

namespace NetCoreExamples {
    class ScanConnect {
        static string ScanForMetaWear() {
            var devices = new List<ScanResult>();
            var seen = new HashSet<string>();
            // Set a handler to process scanned devices     
            Scanner.OnResultReceived = item => {
                // Filter devices that do not advertise the MetaWear service or have already been seen
                if (item.HasServiceUuid(Constants.METAWEAR_GATT_SERVICE.ToString()) && !seen.Contains(item.Mac)) {
                    seen.Add(item.Mac);

                    Console.WriteLine($"[{devices.Count}] = {item.Mac} ({item.Name})");
                    devices.Add(item);
                }
            };

            int selection;
            do {
                seen.Clear();
                devices.Clear();

                Console.WriteLine("Scanning for devices...");
                Scanner.Start();

                Console.WriteLine("Press [Enter] to stop the scan");
                Console.ReadLine();
                Scanner.Stop();

                Console.Write("Select your device (-1 to rescan): ");
                selection = int.Parse(Console.ReadLine());
                // Repeat until user selects a device
            } while (selection == -1);

            // return selected mac address
            return devices[selection].Mac;
        }
        
        internal static async Task<IMetaWearBoard> Connect(string mac, int retries = 2) {
            var metawear = Application.GetMetaWearBoard(mac);

            Console.WriteLine($"Connecting to {metawear.MacAddress}...");
            do {
                try {
                    await metawear.InitializeAsync();
                    retries = -1;
                } catch (Exception e) {
                    Console.WriteLine($"Error connecting to {metawear.MacAddress}, retrying...");
                    Console.WriteLine(e);
                    retries--;
                }
            } while (retries > 0);

            if (retries == 0) {
                throw new ApplicationException($"Failed to connect to {metawear.MacAddress} after {retries + 1} attempts");
            } else {
                Console.WriteLine($"Connected to {metawear.MacAddress}");
                return metawear;
            }
        }

        static async Task RunAsync(string[] args) {
            var metawear = await Connect(ScanForMetaWear());
            Console.WriteLine($"Device information: {await metawear.ReadDeviceInformationAsync()}");
            await metawear.DisconnectAsync();
        }
    }
}