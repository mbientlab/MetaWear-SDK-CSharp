using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ForcedDataProducer : DataProducer, IForcedDataProducer {
        internal ForcedDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(dataTypeBase, bridge) {
        }

        public void Read() {
            dataTypeBase.read(bridge);
        }
    }
}
