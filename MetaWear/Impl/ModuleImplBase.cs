using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    abstract class ModuleImplBase : SerializableType {
        internal ModuleImplBase(IModuleBoardBridge bridge) : base(bridge) {
            init();
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            base.restoreTransientVars(bridge);
            init();
        }

        protected virtual void init() { }
        internal virtual void aggregateDataType(ICollection<DataTypeBase> collection) { }
        public virtual void tearDown() { }
        public virtual void disconnected() { }
    }
}
