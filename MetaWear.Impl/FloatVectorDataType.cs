using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    abstract class FloatVectorDataType : DataTypeBase {
        internal FloatVectorDataType(Module module, byte register, DataAttributes attributes) : 
            base(module, register, attributes) { }

        internal FloatVectorDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) : 
            base(input, module, register, id, attributes) { }
    }
}
