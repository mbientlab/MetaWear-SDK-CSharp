using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Exception indicating that an invalid combination of route components was used.
    /// </summary>
    public class IllegalRouteOperationException : InvalidOperationException {
        public IllegalRouteOperationException(string message) : base(message) { }
        public IllegalRouteOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
