using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class FloatDataType : DataTypeBase {
        private class FloatData : DataBase {
            private readonly bool logData;

            public FloatData(bool logData, DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) : 
                    base(bridge, datatype, timestamp, bytes) {
                this.logData = logData;
            }

            public override Type[] Types => new Type[] { typeof(float) };

            public override T Value<T>() {
                var type = typeof(T);

                int offset = logData ? 0 : datatype.attributes.offset;

                if (type == typeof(float)) {
                    if (datatype.attributes.signed) {
                        switch (Math.Min(datatype.attributes.unitLength(), bytes.Length - offset)) {
                            case 1:
                                return (T)Convert.ChangeType((sbyte) (bytes[offset]) / Scale, type);
                            case 2:
                                return (T)Convert.ChangeType(BitConverter.ToInt16(bytes, offset) / Scale, type);
                            case 3:
                                byte[] padded = Util.pad(bytes, true);
                                return (T)Convert.ChangeType(BitConverter.ToInt32(padded, offset) / Scale, type);
                            case 4:
                                return (T)Convert.ChangeType(BitConverter.ToInt32(bytes, offset) / Scale, type);
                        }
                    } else {
                        switch (Math.Min(datatype.attributes.unitLength(), bytes.Length - offset)) {
                            case 1:
                                return (T)Convert.ChangeType(bytes[offset] / Scale, type);
                            case 2:
                                return (T)Convert.ChangeType(BitConverter.ToUInt16(bytes, offset) / Scale, type);
                            case 3:
                                byte[] padded = Util.pad(bytes, false);
                                return (T)Convert.ChangeType(BitConverter.ToUInt32(padded, offset) / Scale, type);
                            case 4:
                                return (T)Convert.ChangeType(BitConverter.ToUInt32(bytes, offset) / Scale, type);
                        }
                    }
                }

                return base.Value<T>();
            }
        }

        internal FloatDataType(Module module, byte register, byte id, DataAttributes attributes) : 
                base(module, register, id, attributes) { }

        internal FloatDataType(Module module, byte register, DataAttributes attributes) :
                base(module, register, attributes) { }

        internal FloatDataType(DataTypeBase input, Module module, byte register, DataAttributes attributes) :
                this(input, module, register, NO_ID, attributes) { }

        internal FloatDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

        public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
            return new FloatData(logData, this, bridge, timestamp, data);
        }

        public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            return new FloatDataType(input, module, register, id, attributes);
        }
    }
}
