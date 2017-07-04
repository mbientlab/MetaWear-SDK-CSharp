using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ModuleImplBase : SerializableType {
        internal ModuleImplBase(IModuleBoardBridge bridge) : base(bridge) {
            init();
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            base.restoreTransientVars(bridge);
            init();
        }

        protected virtual void init() { }
        public virtual void tearDown() { }
        public virtual void disconnected() { }
    }
}
