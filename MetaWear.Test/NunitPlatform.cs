using MbientLab.MetaWear.Impl.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using NUnit.Framework;

namespace MbientLab.MetaWear.Test {
    class ByteArrayComparer : EqualityComparer<byte[]> {
        public override bool Equals(byte[] first, byte[] second) {
            if (first == null || second == null) {
                // null == null returns true.
                // non-null == null returns false.
                return first == second;
            }
            if (ReferenceEquals(first, second)) {
                return true;
            }
            if (first.Length != second.Length) {
                return false;
            }
            // Linq extension method is based on IEnumerable, must evaluate every item.
            return first.SequenceEqual(second);
        }
        public override int GetHashCode(byte[] obj) {
            if (obj == null) {
                throw new ArgumentNullException("obj");
            }

            int value = 0;
            for(int i = 0; i < Math.Min(4, obj.Length); i++) {
                value |= obj[i] << (i * 8);
            }

            return value;
        }
    }

    internal class NunitPlatform : IBluetoothLeGatt, ILibraryIO {
        const int RESPONSE_DELAY = 20;

        internal string fileSuffix;
        internal int nDisconnects;
        internal bool delayModuleDiscovery = false, enableMetaBoot = false, serializeModuleInfo = false, deserializeModuleInfo = false;
        internal InitializeResponse initResponse;
        internal byte maxProcessors = 28, maxLoggers = 8, maxTimers = 8, maxEvents = 28;
        internal byte timerId = 0, eventId = 0, loggerId = 0, dataProcessorId = 0, macroId = 0;
        internal Dictionary<byte[], byte[]> customResponses = new Dictionary<byte[], byte[]>(new ByteArrayComparer());

        internal List<byte[]> connectCommands = new List<byte[]>();
        internal List<byte[]> commands = new List<byte[]>();
        internal List<GattCharWriteType> writeTypes = new List<GattCharWriteType>();

        internal Action<byte[]> charChangedHandler = null;

        public ulong BluetoothAddress {
            get {
                return 0xCBB749BF2733;
            }
        }

        public Action OnDisconnect { get; set; }

        public NunitPlatform(InitializeResponse initResponse) {
            this.initResponse = initResponse;
        }

        public Task<bool> ServiceExistsAsync(Guid serviceUuid) {
            return Task.FromResult(serviceUuid.Equals(Constants.METABOOT_SERVICE) && enableMetaBoot);
        }
        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar) {
            if (gattChar.Equals(DeviceInformationService.FIRMWARE_REVISION)) {
                await Task.Delay(RESPONSE_DELAY);
                return initResponse.firmwareRevision;
            }
            if (gattChar.Equals(DeviceInformationService.MODEL_NUMBER)) {
                await Task.Delay(RESPONSE_DELAY);
                return initResponse.modelNumber;
            }
            if (gattChar.Equals(DeviceInformationService.HARDWARE_REVISION)) {
                await Task.Delay(RESPONSE_DELAY);
                return initResponse.hardwareRevision;
            }
            if (gattChar.Equals(DeviceInformationService.SERIAL_NUMBER)) {
                await Task.Delay(RESPONSE_DELAY);
                return initResponse.serialNumber;
            }
            if (gattChar.Equals(DeviceInformationService.MANUFACTURER_NAME)) {
                await Task.Delay(RESPONSE_DELAY);
                return initResponse.manufacturer;
            }
            if (gattChar.Equals(BatteryService.BATTERY_LEVEL)) {
                await Task.Delay(RESPONSE_DELAY);
                return new byte[] { 99 };
            }

            return null;
        }
        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value) {
            if (value[1] == 0x80) {
                connectCommands.Add(value);
                if (initResponse.moduleResponses.TryGetValue(value[0], out var response)) {
                    if (delayModuleDiscovery) {
                        await Task.Delay(RESPONSE_DELAY);    
                    }
                    charChangedHandler(response);
                }
            } else {
                byte[] response = null;

                if (value[0] == 0xb && value[1] == 0x84) {
                    connectCommands.Add(value);
                    response = customResponses.ContainsKey(value) ? customResponses[value] : new byte[] { 0x0b, 0x84, 0x15, 0x04, 0x00, 0x00, 0x05 };
                } else { 
                    commands.Add(value);
                    writeTypes.Add(writeType);

                    if (customResponses.ContainsKey(value)) {
                        response = customResponses[value];
                    } else if (loggerId < maxLoggers && value[0] == 0xb && value[1] == 0x2) {
                        response = new byte[] { value[0], 0x2, loggerId };
                        loggerId++;
                    } else if (dataProcessorId < maxProcessors && value[0] == 0x9 && value[1] == 0x2) {
                        response = new byte[] { value[0], 0x2, dataProcessorId };
                        dataProcessorId++;
                    } else if (eventId < maxEvents && value[0] == 0xa && value[1] == 0x3) {
                        response = new byte[] { value[0], 0x2, eventId };
                        eventId++;
                    } else if (timerId < maxTimers && value[0] == 0xc && value[1] == 0x2) {
                        response = new byte[] { value[0], 0x2, timerId };
                        timerId++;
                    } else if (value[0] == 0xf && value[1] == 0x2) {
                        response = new byte[] { value[0], 0x2, macroId };
                        macroId++;
                    } else if (value[0] == 0xb && value[1] == 0x85) {
                        sendMockResponse(new byte[] { 0x0b, 0x85, 0x9e, 0x01, 0x00, 0x00 });
                    } else if (value[0] == 0xfe && (value[1] == 0x1 || value[1] == 0x6 || value[1] == 0x2)) {
                        await Task.Delay(1000);

                        nDisconnects++;
                        OnDisconnect();
                    }   
                }

                if (response != null) {
                    await Task.Delay(RESPONSE_DELAY);
                    charChangedHandler(response);
                }
            }
        }

        public byte[][] GetConnectCommands() {
            return connectCommands.ToArray<byte[]>();
        }
        public byte[][] GetCommands() {
            return commands.ToArray<byte[]>();
        }

        public byte[] GetLastCommand() {
            return commands[commands.Count - 1];
        }

        public void sendMockResponse(byte[] response) {
            charChangedHandler?.Invoke(response);
        }

        public void sendMockResponse(sbyte[] response) {
            byte[] converted = new byte[response.Length];
            int i = 0;
            
            foreach (var b in response) {
                converted[i++] = (byte)(b & 0xff);
            }
            charChangedHandler?.Invoke(converted);
        }

        public Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler) {
            if (enableMetaBoot && !gattChar.Item1.Equals(Constants.METABOOT_SERVICE)) {
                throw new InvalidOperationException(string.Format("Service '{0}' does not exist", gattChar.Item1.ToString()));
            }
            charChangedHandler = handler;
            return Task.CompletedTask;
        }

        public void LogWarn(string tag, string message, Exception e) {
            Console.WriteLine(string.Format("{0}: {1}\r\n{2}", tag, message, e.StackTrace));
        }

        public Task LocalSaveAsync(string key, byte[] data) {
            String suffix = key.Substring(key.LastIndexOf(".") + 1).ToLower();
            if (!suffix.Equals("board_attr") || serializeModuleInfo) {
                using (Stream outs = File.Open(Path.Combine(TestContext.CurrentContext.TestDirectory, suffix + "_" + fileSuffix), FileMode.Create)) {
                    outs.Write(data, 0, data.Length);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<Stream> LocalLoadAsync(string key) {
            String suffix = key.Substring(key.LastIndexOf(".") + 1).ToLower();
            if (!suffix.Equals("board_attr") || deserializeModuleInfo) {
                return await Task.FromResult(OpenFile(suffix + "_" + fileSuffix, FileMode.Open));
            }
            return await Task.FromResult<Stream>(null);
        }

        private Stream OpenFile(string filename, FileMode mode) {
            var directory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(TestContext.CurrentContext.TestDirectory)));
            return File.Open(Path.Combine(directory, "res", filename), mode);
        }

        public void ReadFile(string filename, Action<string> handler) {
            using (var stream = new StreamReader(OpenFile(filename, FileMode.Open))) {
                string line;
                while ((line = stream.ReadLine()) != null) {
                    handler(line);
                }
            }
        }

        public Task DiscoverServicesAsync() {
            return Task.FromResult(true);
        }

        public Task DisconnectAsync() {
            nDisconnects++;
            OnDisconnect();
            return Task.FromResult(true);
        }
    }
}
