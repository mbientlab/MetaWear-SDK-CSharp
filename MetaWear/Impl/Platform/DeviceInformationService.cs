using System;

namespace MbientLab.MetaWear.Impl.Platform {
    /// <summary>
    /// Manufacturer and/or vendor information about a device
    /// </summary>
    public class DeviceInformationService {
        private static readonly Guid SERVICE_GUID = new Guid("0000180a-0000-1000-8000-00805f9b34fb");

        /// <summary>
        /// Revision for the firmware within the device
        /// </summary>
        public static readonly Tuple<Guid, Guid> FIRMWARE_REVISION = Tuple.Create(SERVICE_GUID, new Guid("00002a26-0000-1000-8000-00805f9b34fb"));
        /// <summary>
        /// Model number that is assigned by the device
        /// </summary>
        public static readonly Tuple<Guid, Guid> MODEL_NUMBER = Tuple.Create(SERVICE_GUID, new Guid("00002a24-0000-1000-8000-00805f9b34fb"));
        /// <summary>
        /// Revision for the hardware within the device
        /// </summary>
        public static readonly Tuple<Guid, Guid> HARDWARE_REVISION = Tuple.Create(SERVICE_GUID, new Guid("00002a27-0000-1000-8000-00805f9b34fb"));
        /// <summary>
        /// Name of the manufacturer of the device
        /// </summary>
        public static readonly Tuple<Guid, Guid> MANUFACTURER_NAME = Tuple.Create(SERVICE_GUID, new Guid("00002a29-0000-1000-8000-00805f9b34fb"));
        /// <summary>
        /// Serial number for a particular instance of the device
        /// </summary>
        public static readonly Tuple<Guid, Guid> SERIAL_NUMBER = Tuple.Create(SERVICE_GUID, new Guid("00002a25-0000-1000-8000-00805f9b34fb"));
    }
}
