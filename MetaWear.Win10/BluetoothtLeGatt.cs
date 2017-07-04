using MbientLab.MetaWear.Platform;

using System;
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

        public async Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler) {
            charChangedHandler = handler;

            if (notifyChar == null) {
                var serviceResult = await device.GetGattServicesForUuidAsync(gattChar.Item1, BluetoothCacheMode.Uncached);
                if (serviceResult.Services.Count == 0) {
                    throw new InvalidOperationException(string.Format("GATT service '{0}' does not exist", gattChar.Item1));
                }

                var charResult = await serviceResult.Services.FirstOrDefault().GetCharacteristicsForUuidAsync(gattChar.Item2);
                if (charResult.Characteristics.Count == 0) {
                    throw new InvalidOperationException(string.Format("GATT characteristic '{0}' does not exist", gattChar.Item2));
                }

                notifyChar = charResult.Characteristics.FirstOrDefault();

                var notifyResult = await notifyChar.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (notifyResult != GattCommunicationStatus.Success) {
                    throw new InvalidOperationException(string.Format("Failed to enable notifications (status = {0:d})", (int)notifyResult));
                }
                notifyChar.ValueChanged += notifyHandler;
            }
        }

        public async Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar) {
            var serviceResult = await device.GetGattServicesForUuidAsync(gattChar.Item1);
            var charResult = await serviceResult.Services.FirstOrDefault().GetCharacteristicsForUuidAsync(gattChar.Item2);
            var readResult = await charResult.Characteristics.FirstOrDefault().ReadValueAsync();
            
            if (readResult.Status == GattCommunicationStatus.Success) {
                return readResult.Value.ToArray();
            }
            throw new InvalidOperationException("Failed to read value from GATT characteristic: " + gattChar.Item2.ToString());
        }

        public Task<bool> RemoteDisconnectAsync() {
            dcTaskSource = new TaskCompletionSource<bool>();
            return dcTaskSource.Task;
        }

        public async Task<bool> ServiceExistsAsync(Guid serviceGuid) {
            var result = await device.GetGattServicesForUuidAsync(serviceGuid, BluetoothCacheMode.Uncached);
            return result.Services.Count != 0;
        }

        public async Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value) {
            var serviceResult = await device.GetGattServicesForUuidAsync(gattChar.Item1);
            var charResult = await serviceResult.Services.FirstOrDefault().GetCharacteristicsForUuidAsync(gattChar.Item2);
            var status = await charResult.Characteristics.FirstOrDefault().WriteValueAsync(value.AsBuffer(), 
                    writeType == GattCharWriteType.WRITE_WITHOUT_RESPONSE ? GattWriteOption.WriteWithoutResponse : GattWriteOption.WriteWithResponse);

            if (status != GattCommunicationStatus.Success) {
                throw new InvalidOperationException("Failed to write value to GATT characteristic: " + gattChar.Item2.ToString());
            }
        }

        private void notifyHandler(GattCharacteristic gattCharChanged, GattValueChangedEventArgs obj) {
            charChangedHandler(obj.CharacteristicValue.ToArray());
        }
    }
}
