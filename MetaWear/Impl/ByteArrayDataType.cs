using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ByteArrayDataType : DataTypeBase {
        private class ByteArrayData : DataBase {
            public ByteArrayData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) : 
                base(bridge, datatype, timestamp, bytes) {
            }

            public override Type[] Types => new Type[] { typeof(byte[]) };

            public override T Value<T>() {
                var type = typeof(T);

                if (type == typeof(byte[])) {
                    return (T)Convert.ChangeType(bytes, type);
                }
                return base.Value<T>();
            }
        }

        internal ByteArrayDataType(Module module, byte register, byte id, DataAttributes attributes) : 
                base(module, register, id, attributes) { }

        internal ByteArrayDataType(Module module, byte register, DataAttributes attributes) :
                base(module, register, attributes) { }

        internal ByteArrayDataType(DataTypeBase input, Module module, byte register, DataAttributes attributes) :
                this(input, module, register, NO_ID, attributes) { }

        private ByteArrayDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

        public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
            return new ByteArrayData(bridge, this, timestamp, data);
        }

        public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            return new ByteArrayDataType(input, module, register, id, attributes);
        }

    }
}
