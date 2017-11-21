using MbientLab.MetaWear.Platform;

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
        private TaskCompletionSource<bool> dcTaskSource;
        private Dictionary<Guid, GattCharacteristic> characteristics = new Dictionary<Guid, GattCharacteristic>();

        public ulong BluetoothAddress { get {
                return device.BluetoothAddress;
            }
        }

        public Action<bool> OnDisconnect { get; set ; }

        public BluetoothLeGatt(BluetoothLEDevice device) {
            this.device = device;
            device.ConnectionStatusChanged += (sender, args) => {
                switch (sender.ConnectionStatus) {
                    case BluetoothConnectionStatus.Disconnected:
                        if (notifyChar != null) {
                            notifyChar.ValueChanged -= notifyHandler;
                            notifyChar = null;
                        }
                        characteristics.Clear();

                        if (dcTaskSource != null) {
                            dcTaskSource.SetResult(true);
                            OnDisconnect(false);
                        } else {
                            OnDisconnect(true);
                        }
                        break;
                    case BluetoothConnectionStatus.Connected:
                        break;
                }
            };
        }

        private async Task DiscoverCharacteristicsAsync() {
            if (characteristics.Count == 0) {
                var servicesResult = await device.GetGattServicesAsync();
                foreach (var service in servicesResult.Services) {
                    var charsresult = await service.GetCharacteristicsAsync();
                    foreach (var characteristic in charsresult.Characteristics) {
                        characteristics.Add(characteristic.Uuid, characteristic);
                    }
                }

                if (characteristics.Count == 0) {
                    throw new InvalidOperationException("No GATT characteristics were discovered");
                }
            }
        }

        public async Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler) {
            await DiscoverCharacteristicsAsync();

            charChangedHandler = handler;

            if (notifyChar == null) {
                if (characteristics.TryGetValue(gattChar.Item2, out var notify)) {
                    var result = await notify.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (result != GattCommunicationStatus.Success) {
                        throw new InvalidOperationException(string.Format("Failed to enable notifications (status = {0:d})", (int)result));
                    }
                    notifyChar = notify;
                    notifyChar.ValueChanged += notifyHandler;
                } else {
                    throw new InvalidOperationException(string.Format("GATT characteristic '{0}' does not exist", gattChar.Item2));
                }
            }
        }

        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar) {
            await DiscoverCharacteristicsAsync();

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

        public Task RemoteDisconnectAsync() {
            dcTaskSource = new TaskCompletionSource<bool>();
            return dcTaskSource.Task;
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceGuid) {
            var result = await device.GetGattServicesForUuidAsync(serviceGuid, BluetoothCacheMode.Uncached);
            return result.Services.Count != 0;
        }

        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value) {
            await DiscoverCharacteristicsAsync();

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

        private void notifyHandler(GattCharacteristic gattCharChanged, GattValueChangedEventArgs obj) {
            charChangedHandler(obj.CharacteristicValue.ToArray());
        }
    }
}
