using MbientLab.MetaWear.Data;
using System;

namespace MbientLab.MetaWear.Impl {
    abstract class DataBase : IData {
        internal readonly byte[] bytes;

        internal readonly IModuleBoardBridge bridge;
        internal readonly DataTypeBase datatype;

        internal Func<Type, object> extraFn;

        internal DataBase(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) {
            Timestamp = timestamp;
            this.bytes = bytes;
            this.bridge = bridge;
            this.datatype = datatype;
            extraFn = type => null;
        }

        public byte[] Bytes {
            get {
                byte[] copy = new byte[bytes.Length];
                Array.Copy(bytes, copy, bytes.Length);

                return copy;
            }
        }
        public DateTime Timestamp { get; private set; }
        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        public virtual float Scale => datatype.scale(bridge);
        public abstract Type[] Types { get; }

        public virtual T Value<T>() {
            throw new InvalidCastException(string.Format("Cannot cast value to: \'{0}\'", typeof(T).Name));
        }

        public override string ToString() {
            return string.Format("{{timestamp: {0}, data: {1}{2}", FormattedTimestamp, Util.ArrayToHexString(Bytes), "}");
        }

        public T Extra<T>() {
            var type = typeof(T);
            return (T) Convert.ChangeType(extraFn(type), type);
        }
    }

    class AngularVelocityData : DataBase {
        public AngularVelocityData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) : 
            base(bridge, datatype, timestamp, bytes) {
        }

        public override Type[] Types => new Type[] { typeof(AngularVelocity), typeof(FloatVector) };

        public override T Value<T>() {
            var type = typeof(T);

            if (type == typeof(AngularVelocity) || type == typeof(FloatVector)) {
                return (T)Convert.ChangeType(new AngularVelocity(
                    BitConverter.ToInt16(bytes, 0) / Scale,
                    BitConverter.ToInt16(bytes, 2) / Scale,
                    BitConverter.ToInt16(bytes, 4) / Scale), typeof(AngularVelocity));
            }
            return base.Value<T>();
        }
    }

    class MagneticFieldData : DataBase {
        public MagneticFieldData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) :
            base(bridge, datatype, timestamp, bytes) {
        }

        public override Type[] Types => new Type[] { typeof(MagneticField), typeof(FloatVector) };

        public override T Value<T>() {
            var type = typeof(T);

            if (type == typeof(MagneticField) || type == typeof(FloatVector)) {
                return (T)Convert.ChangeType(new MagneticField(
                    BitConverter.ToInt16(bytes, 0) / Scale,
                    BitConverter.ToInt16(bytes, 2) / Scale,
                    BitConverter.ToInt16(bytes, 4) / Scale), typeof(MagneticField));
            }
            return base.Value<T>();
        }
    }

    class QuaternionData : DataBase {
        public QuaternionData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) :
            base(bridge, datatype, timestamp, bytes) {
        }

        public override Type[] Types => new Type[] { typeof(Quaternion), typeof(FloatVector) };

        public override T Value<T>() {
            var type = typeof(T);

            if (type == typeof(Quaternion) || type == typeof(FloatVector)) {
                return (T)Convert.ChangeType(new Quaternion(
                    BitConverter.ToSingle(bytes, 0),
                    BitConverter.ToSingle(bytes, 4),
                    BitConverter.ToSingle(bytes, 8),
                    BitConverter.ToSingle(bytes, 12)), typeof(Quaternion));
            }
            return base.Value<T>();
        }
    }

    class EulerAnglesData : DataBase {
        public EulerAnglesData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) :
            base(bridge, datatype, timestamp, bytes) {
        }

        public override Type[] Types => new Type[] { typeof(EulerAngles), typeof(FloatVector) };

        public override T Value<T>() {
            var type = typeof(T);

            if (type == typeof(EulerAngles) || type == typeof(FloatVector)) {
                return (T)Convert.ChangeType(new EulerAngles(
                    BitConverter.ToSingle(bytes, 0),
                    BitConverter.ToSingle(bytes, 4),
                    BitConverter.ToSingle(bytes, 8),
                    BitConverter.ToSingle(bytes, 12)), typeof(EulerAngles));
            }
            return base.Value<T>();
        }
    }
}
