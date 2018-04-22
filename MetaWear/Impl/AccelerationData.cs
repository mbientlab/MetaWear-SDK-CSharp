using MbientLab.MetaWear.Data;
using System;

namespace MbientLab.MetaWear.Impl {
    internal class AccelerationData : DataBase {
        public AccelerationData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) : 
            base(bridge, datatype, timestamp, bytes) {
        }

        public override Type[] Types => new Type[] { typeof(Acceleration), typeof(FloatVector) };

        public override T Value<T>() {
            var type = typeof(T);

            if (type == typeof(Acceleration) || type == typeof(FloatVector)) {
                return (T) Convert.ChangeType(new Acceleration(
                    BitConverter.ToInt16(bytes, 0) / Scale, 
                    BitConverter.ToInt16(bytes, 2) / Scale, 
                    BitConverter.ToInt16(bytes, 4) / Scale), typeof(Acceleration));
            }
            return base.Value<T>();
        }
    }
}
