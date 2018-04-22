using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(DataTypeBase))]
    [DataContract]
    abstract class DeviceDataConsumer {
        [DataMember] internal readonly DataTypeBase source;
        [DataMember] internal string name;
        internal Action<IData> handler;

        public DeviceDataConsumer(DataTypeBase source) {
            this.source = source;
        }

        public DeviceDataConsumer(DataTypeBase source, Action<IData> handler) : this(source) {
            this.handler = handler;
        }

        public void call(IData data) {
            handler?.Invoke(data);
        }

        public abstract void enableStream(IModuleBoardBridge bridge);
        public abstract void disableStream(IModuleBoardBridge bridge);
        public abstract void addDataHandler(IModuleBoardBridge bridge);
    }
}
