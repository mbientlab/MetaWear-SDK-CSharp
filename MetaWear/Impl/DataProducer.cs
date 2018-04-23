using System;
using System.Threading.Tasks;
using MbientLab.MetaWear.Builder;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(IntegralDataType))]
    [KnownType(typeof(MilliUnitsFloatDataType))]
    [DataContract]
    class DataProducer : SerializableType, IDataProducer {
        [DataMember] protected readonly DataTypeBase dataTypeBase;

        internal DataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(bridge) {
            this.dataTypeBase = dataTypeBase;
        }

        public Task<IRoute> AddRouteAsync(Action<IRouteComponent> builder) {
            return bridge.queueRouteBuilder(builder, dataTypeBase);
        }
    }
}
