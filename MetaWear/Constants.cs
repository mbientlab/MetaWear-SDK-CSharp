using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// API related constants
    /// </summary>
    public class Constants {
        /// <summary>
        /// UUID identifying the MetaWear GATT service and the advertising UUID
        /// </summary>
        public static readonly Guid METAWEAR_GATT_SERVICE = new Guid("326A9000-85CB-9195-D9DD-464CFBBAE75A");
        /// <summary>
        /// UUID identifying a MetaWear board in MetaBoot mode.  A MetaWear board advertising with this UUID indicates 
        /// it is in MetaBoot mode.
        /// </summary>
        public static readonly Guid METABOOT_SERVICE = new Guid("00001530-1212-efde-1523-785feabcd123");
    }
}
