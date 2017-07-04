using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class MilliUnitsFloatDataType : FloatDataType {
        internal MilliUnitsFloatDataType(Module module, byte register, DataAttributes attributes) :
                base(module, register, attributes) { }

        internal MilliUnitsFloatDataType(Module module, byte register, byte id, DataAttributes attributes) : 
                base(module, register, id, attributes) { }

        internal MilliUnitsFloatDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

        public override float scale(IModuleBoardBridge bridge) {
            return 1000f;
        }

        public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            return new MilliUnitsFloatDataType(input, module, register, id, attributes);
        }
    }
}
