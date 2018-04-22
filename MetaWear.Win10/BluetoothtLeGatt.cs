using MbientLab.MetaWear.Impl.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace MbientLab.MetaWear.Win10 {
    internal class BluetoothLeGatt : IBluetoothLeGatt {
        private GattCharacteristic notifyChar = null;
        private BluetoothLEDevice device;
        private Action<byte[]> charChangedHandler; 
        private Dictionary<Guid, GattCharacteristic> characteristics = new Dictionary<Guid, GattCharacteristic>();

        public ulong BluetoothAddress => device.BluetoothAddress;
        public Action OnDisconnect { get; set ; }

        public BluetoothLeGatt(BluetoothLEDevice device) {
            this.device = device;
            device.ConnectionStatusChanged += (sender, args) => {
                switch (sender.ConnectionStatus) {
                    case BluetoothConnectionStatus.Disconnected:
                        ResetCharacteristics();
                        OnDisconnect();
                        break;
                    case BluetoothConnectionStatus.Connected:
                        break;
                }
            };
        }

        private void ResetCharacteristics() {
            if (notifyChar != null) {
                notifyChar.ValueChanged -= NotifyHandler;
                notifyChar = null;
            }
            characteristics.Clear();
        }

        public async Task DiscoverServicesAsync() {
            ResetCharacteristics();

            int retry = 3;
            while (retry >= 0) {
                var servicesResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                foreach (var service in servicesResult.Services) {
                    var charsresult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    foreach (var characteristic in charsresult.Characteristics) {
                        characteristics.Add(characteristic.Uuid, characteristic);
                    }
                }

                if (characteristics.Count == 0) {
                    retry--;
                } else {
                    retry = -1;
                }
            }

            if (characteristics.Count == 0) {
                throw new InvalidOperationException("No GATT characteristics were discovered");
            }
        }

        public async Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler) {
            charChangedHandler = handler;

            if (notifyChar == null) {
                if (characteristics.TryGetValue(gattChar.Item2, out var notify)) {
                    var result = await notify.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (result != GattCommunicationStatus.Success) {
                        throw new InvalidOperationException(string.Format("Failed to enable notifications (status = {0:d})", (int)result));
                    }
                    notifyChar = notify;
                    notifyChar.ValueChanged += NotifyHandler;
                } else {
                    throw new InvalidOperationException(string.Format("GATT characteristic '{0}' does not exist", gattChar.Item2));
                }
            }
        }

        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar) {
            if (characteristics.TryGetValue(gattChar.Item2, out var characteristic)) {
                var result = await characteristic.ReadValueAsync();

                if (result.Status == GattCommunicationStatus.Success) {
                    return result.Value.ToArray();
                }
                throw new InvalidOperationException("Failed to read value from GATT characteristic: " + gattChar.Item2.ToString());
            } else {
                throw new InvalidOperationException(string.Format("GATT characteristic '{0}' does not exist", gattChar.Item2));
            }
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceGuid) {
            var result = await device.GetGattServicesForUuidAsync(serviceGuid, BluetoothCacheMode.Uncached);
            return result.Services.Count != 0;
        }

        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value) {
            if (characteristics.TryGetValue(gattChar.Item2, out var characteristic)) {
                var result = await characteristic.WriteValueAsync(value.AsBuffer(), 
                    writeType == GattCharWriteType.WRITE_WITHOUT_RESPONSE ? GattWriteOption.WriteWithoutResponse : GattWriteOption.WriteWithResponse);

                if (result != GattCommunicationStatus.Success) {
                    throw new InvalidOperationException("Failed to write value to GATT characteristic: " + gattChar.Item2.ToString());
                }
            } else {
                throw new InvalidOperationException(string.Format("GATT characteristic '{0}' does not exist", gattChar.Item2));
            }
        }

        private void NotifyHandler(GattCharacteristic gattCharChanged, GattValueChangedEventArgs obj) {
            charChangedHandler(obj.CharacteristicValue.ToArray());
        }

        public Task DisconnectAsync() {
            throw new NotSupportedException("Use 'IDebug.DisconnectAsync()' to close the connection");
        }

        internal void Close() {
            foreach(var s in new HashSet<GattDeviceService>(characteristics.Values.Select(e => e.Service))) {
                s.Dispose();
            }
            characteristics.Clear();

            device.Dispose();
            device = null;
        }
    }
}
