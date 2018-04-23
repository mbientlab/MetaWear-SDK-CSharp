using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class IntegralDataType : DataTypeBase {
        private class IntegralData : DataBase {
            private readonly bool logData;

            public IntegralData(bool logData, IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) : 
                    base(bridge, datatype, timestamp, bytes) {
                this.logData = logData;
            }

            public override Type[] Types {
                get {
                    int offset = logData ? 0 : datatype.attributes.offset;
                    int realLength = bytes.Length - offset;

                    switch (realLength) {
                        case 1:
                            return new Type[] { typeof(byte), typeof(sbyte) };
                        case 2:
                            return new Type[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort) };
                        case 4:
                            return new Type[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint) };
                        default:
                            throw new InvalidOperationException(string.Format("data length ({0} bytes) unsupported", realLength));
                    }
                }
            }

            public override T Value<T>() {
                var type = typeof(T);

                int offset = logData ? 0 : datatype.attributes.offset;

                if (type == typeof(byte) || type == typeof(sbyte)) {
                    return (T) Convert.ChangeType(bytes[0], type);
                }
                if (type == typeof(short)) {
                    return (T)Convert.ChangeType(BitConverter.ToInt16(bytes, offset), type);
                }
                if (type == typeof(ushort)) {
                    return (T)Convert.ChangeType(BitConverter.ToUInt16(bytes, offset), type);
                }
                if (type == typeof(int)) {
                    return (T)Convert.ChangeType(BitConverter.ToInt32(bytes, offset), type);
                }
                if (type == typeof(uint)) {
                    return (T)Convert.ChangeType(BitConverter.ToUInt32(bytes, offset), type);
                }

                return base.Value<T>();
            }
        }

        internal IntegralDataType(Module module, byte register, byte id, DataAttributes attributes) : 
                base(module, register, id, attributes) { }

        internal IntegralDataType(Module module, byte register, DataAttributes attributes) :
                base(module, register, attributes) { }

        internal IntegralDataType(DataTypeBase input, Module module, byte register, DataAttributes attributes) :
                this(input, module, register, NO_ID, attributes) { }

        internal IntegralDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

        public override float scale(IModuleBoardBridge bridge) {
            return 1f;
        }
        public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
            return new IntegralData(logData, bridge, this, timestamp, data);
        }

        public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            return new IntegralDataType(input, module, register, id, attributes);
        }
    }
}
