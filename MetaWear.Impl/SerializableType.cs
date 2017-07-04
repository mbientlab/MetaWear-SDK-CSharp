using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class SerializableType {
        protected IModuleBoardBridge bridge;

        internal SerializableType(IModuleBoardBridge bridge) {
            this.bridge = bridge;
        }

        internal virtual void restoreTransientVars(IModuleBoardBridge bridge) {
            this.bridge = bridge;
        }
    }
}
