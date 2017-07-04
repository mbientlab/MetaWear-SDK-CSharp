using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(DataTypeBase))]
    [DataContract]
    abstract class DeviceDataConsumer {
        [DataMember] internal readonly DataTypeBase source;
        internal Action<IData> subscriber;

        public DeviceDataConsumer(DataTypeBase source) {
            this.source = source;
        }

        public DeviceDataConsumer(DataTypeBase source, Action<IData> subscriber) : this(source) {
            this.subscriber = subscriber;
        }

        public void call(IData data) {
            subscriber?.Invoke(data);
        }

        public abstract void enableStream(IModuleBoardBridge bridge);
        public abstract void disableStream(IModuleBoardBridge bridge);
        public abstract void addDataHandler(IModuleBoardBridge bridge);
    }
}
