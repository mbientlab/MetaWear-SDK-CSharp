using MbientLab.MetaWear.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using NUnit.Framework;

namespace MbientLab.MetaWear.Test {
    internal class NunitPlatform : IBluetoothLeGatt, ILibraryIO {
        internal string fileSuffix;
        internal int nDisconnects;
        internal bool delayModuleDiscovery = false, enableMetaBoot = false, serializeModuleInfo = false, deserializeModuleInfo = false;
        internal InitializeResponse initResponse;
        internal byte maxProcessors = 28, maxLoggers = 8, maxTimers = 8, maxEvents = 28;
        internal byte timerId = 0, eventId = 0, loggerId = 0, dataProcessorId = 0, macroId = 0;

        private Queue<byte[]> pendingResponses = new Queue<byte[]>();
        Timer timer;

        List<byte[]> connectCommands = new List<byte[]>();
        List<byte[]> commands = new List<byte[]>();
        List<GattCharWriteType> writeTypes = new List<GattCharWriteType>();

        private Action<byte[]> charChangedHandler;

        public ulong BluetoothAddress {
            get {
                return 0xCBB749BF2733;
            }
        }

        public Action<bool> OnDisconnect { get; set; }

        public NunitPlatform(InitializeResponse initResponse) {
            this.initResponse = initResponse;
            timer = new Timer(e => {
                if (pendingResponses.Count != 0) {
                    sendMockResponse(pendingResponses.Dequeue());
                }
            }, null, 0, 20L);
        }

        public Task<bool> ServiceExistsAsync(Guid serviceUuid) {
            return Task.FromResult(serviceUuid.Equals(Constants.METABOOT_SERVICE) && enableMetaBoot);
        }
        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar) {
            if (gattChar.Equals(DeviceInformationService.FIRMWARE_REVISION)) {
                await Task.Delay(20);
                return initResponse.firmwareRevision;
            }
            if (gattChar.Equals(DeviceInformationService.MODEL_NUMBER)) {
                await Task.Delay(20);
                return initResponse.modelNumber;
            }
            if (gattChar.Equals(DeviceInformationService.HARDWARE_REVISION)) {
                await Task.Delay(20);
                return initResponse.hardwareRevision;
            }
            if (gattChar.Equals(DeviceInformationService.SERIAL_NUMBER)) {
                await Task.Delay(20);
                return initResponse.serialNumber;
            }
            if (gattChar.Equals(DeviceInformationService.MANUFACTURER_NAME)) {
                await Task.Delay(20);
                return initResponse.manufacturer;
            }
            if (gattChar.Equals(BatteryService.BATTERY_LEVEL)) {
                await Task.Delay(20);
                return new byte[] { 99 };
            }

            return null;
        }
        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value) {
            if (value[1] == 0x80) {
                connectCommands.Add(value);
                if (delayModuleDiscovery) {
                    pendingResponses.Enqueue(initResponse.moduleResponses[value[0]]);
                } else {
                    charChangedHandler(initResponse.moduleResponses[value[0]]);
                }
            } else if (value[0] == 0xb && value[1] == 0x84) {
                connectCommands.Add(value);
                await Task.Delay(20);
                pendingResponses.Enqueue(new byte[] { 0x0b, 0x84, 0x15, 0x04, 0x00, 0x00, 0x05 });
            } else {
                commands.Add(value);
                writeTypes.Add(writeType);

                if (loggerId < maxLoggers && value[0] == 0xb && value[1] == 0x2) {
                    byte[] response = { value[0], 0x2, loggerId };
                    loggerId++;

                    pendingResponses.Enqueue(response);
                } else if (dataProcessorId < maxProcessors && value[0] == 0x9 && value[1] == 0x2) {
                    byte[] response = { value[0], 0x2, dataProcessorId };
                    dataProcessorId++;

                    pendingResponses.Enqueue(response);
                } else if (eventId < maxEvents && value[0] == 0xa && value[1] == 0x3) {
                    byte[] response = { value[0], 0x2, eventId };
                    eventId++;

                    pendingResponses.Enqueue(response);
                } else if (timerId < maxTimers && value[0] == 0xc && value[1] == 0x2) {
                    byte[] response = { value[0], 0x2, timerId };
                    timerId++;

                    pendingResponses.Enqueue(response);
                } else if (value[0] == 0xf && value[1] == 0x2) {
                    byte[] response = { value[0], 0x2, macroId };
                    macroId++;

                    pendingResponses.Enqueue(response);
                } else if (value[0] == 0xb && value[1] == 0x85) {
                    sendMockResponse(new byte[] { 0x0b, 0x85, 0x9e, 0x01, 0x00, 0x00 });
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

        public void Reset() {
            commands.Clear();
            writeTypes.Clear();

            eventId = 0;
            loggerId = 0;
            dataProcessorId = 0;
            macroId = 0;
            timerId = 0;
            nDisconnects = 0;
        }

        public void sendMockResponse(byte[] response) {
            charChangedHandler(response);
        }

        public void sendMockResponse(sbyte[] response) {
            byte[] converted = new byte[response.Length];
            int i = 0;
            
            foreach (var b in response) {
                converted[i++] = (byte)(b & 0xff);
            }
            charChangedHandler(converted);
        }

        public Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler) {
            if (enableMetaBoot && !gattChar.Item1.Equals(Constants.METABOOT_SERVICE)) {
                throw new InvalidOperationException(string.Format("Service '{0}' does not exist", gattChar.Item1.ToString()));
            }
            charChangedHandler = handler;
            return Task.CompletedTask;
        }

        public Task<bool> RemoteDisconnectAsync() {
            nDisconnects++;
            OnDisconnect(false);
            return Task.FromResult(true);
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
            return File.Open(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(TestContext.CurrentContext.TestDirectory)), "res", filename), mode);
        }

        public void ReadFile(string filename, Action<string> handler) {
            using (var stream = new StreamReader(OpenFile(filename, FileMode.Open))) {
                string line;
                while ((line = stream.ReadLine()) != null) {
                    handler(line);
                }
            }
        }
    }
}
