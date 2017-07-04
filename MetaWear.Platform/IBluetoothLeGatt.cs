using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Platform {
    /// <summary>
    /// GATT characteristic write types
    /// </summary>
    public enum GattCharWriteType {
        WRITE_WITH_RESPONSE,
        WRITE_WITHOUT_RESPONSE
    }
    /// <summary>
    /// Bluetooth GATT operations used by the API, must be implemented by the target platform
    /// </summary>
    public interface IBluetoothLeGatt {
        /// <summary>
        /// Device's mac address, as an unsigned long
        /// </summary>
        ulong BluetoothAddress { get; }
        /// <summary>
        /// Handler to process disconnect events
        /// </summary>
        Action<bool> OnDisconnect { get; set; }

        /// <summary>
        /// Checks if a GATT service exists
        /// </summary>
        /// <param name="serviceGuid">UUID identifying the service to lookup</param>
        /// <returns>True if service exists, false if not</returns>
        Task<bool> ServiceExistsAsync(Guid serviceGuid);

        /// <summary>
        /// Reads the requested characteristic's value
        /// </summary>
        /// <param name="gattChar">Characteristic to read</param>
        /// <returns>Characteristic's value</returns>
        Task<byte[]> ReadCharacteristicAsync(Tuple<Guid, Guid> gattChar);
        /// <summary>
        /// Writes a GATT characteristic and its value to the remote device
        /// </summary>
        /// <param name="gattChar">GATT characteristic to write</param>
        /// <param name="writeType">Type of GATT write to use</param>
        /// <param name="value">Value to be written</param>
        /// <returns>Null</returns>
        Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value);
        /// <summary>
        /// Enable notifications for the characteristic
        /// </summary>
        /// <param name="gattChar">Characteristic to enable notifications for</param>
        /// <param name="handler">Listener for handling characteristic notifications</param>
        /// <returns>Null</returns>
        Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler);

        /// <summary>
        /// Disconnect attempt that will be initiated by the remote device
        /// </summary>
        /// <returns>True when connection is lost</returns>
        Task<bool> RemoteDisconnectAsync();
    }
}
