using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    abstract class FloatVectorDataType : DataTypeBase {
        internal FloatVectorDataType(Module module, byte register, DataAttributes attributes) : 
            base(module, register, attributes) { }

        internal FloatVectorDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) : 
            base(input, module, register, id, attributes) { }

        internal override Tuple<DataTypeBase, DataTypeBase> transform(DataProcessorConfig config, DataProcessor dpModule) {
            switch (config.id) {
                case DataProcessorConfig.CombinerConfig.ID: {
                        DataAttributes attributes = new DataAttributes(new byte[] { this.attributes.sizes[0] }, 1, 0, false);
                        return Tuple.Create<DataTypeBase, DataTypeBase>(new FloatDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes), null);
                    }
            }

            return base.transform(config, dpModule);
        }
    }
}
