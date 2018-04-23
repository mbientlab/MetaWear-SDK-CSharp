using System;

namespace MbientLab.MetaWear.Impl.Platform {
    /// <summary>
    /// Characteristics under the Battery GATT service
    /// </summary>
    public class BatteryService {
        private static readonly Guid SERVICE_GUID = new Guid("0000180f-0000-1000-8000-00805f9b34fb");

        /// <summary>
        /// Battery level characteristic
        /// </summary>
        public static readonly Tuple<Guid, Guid> BATTERY_LEVEL = Tuple.Create(SERVICE_GUID, new Guid("00002a19-0000-1000-8000-00805f9b34fb"));
    }
}