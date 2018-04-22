using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl.Platform {
    /// <summary>
    /// GATT characteristic write types
    /// </summary>
    public enum GattCharWriteType {
        WRITE_WITH_RESPONSE,
        WRITE_WITHOUT_RESPONSE
    }
    /// <summary>
    /// Bluetooth GATT operations used by the API, must be implemented by the target platform.
    /// <para>Before interacting with any of the characteristics, users must first call <see cref="DiscoverServicesAsync"/></para>
    /// </summary>
    public interface IBluetoothLeGatt {
        /// <summary>
        /// Device's mac address, as an unsigned long
        /// </summary>
        ulong BluetoothAddress { get; }
        /// <summary>
        /// Handler to process disconnect events
        /// </summary>
        Action OnDisconnect { get; set; }

        /// <summary>
        /// Discover GATT services and characteristics avaiable on the remote device
        /// </summary>
        /// <returns>Null when discovery is completed</returns>
        Task DiscoverServicesAsync();
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
        /// <returns>Null when the task is completed</returns>
        Task WriteCharacteristicAsync(Tuple<Guid, Guid> gattChar, GattCharWriteType writeType, byte[] value);
        /// <summary>
        /// Enable notifications for the characteristic
        /// </summary>
        /// <param name="gattChar">Characteristic to enable notifications for</param>
        /// <param name="handler">Listener for handling characteristic notifications</param>
        /// <returns>Null when the task is completed</returns>
        Task EnableNotificationsAsync(Tuple<Guid, Guid> gattChar, Action<byte[]> handler);

        /// <summary>
        /// Closes the Bluetooth LE connection
        /// </summary>
        /// <returns>Null when connection is lost</returns>
        Task DisconnectAsync();
    }
}
