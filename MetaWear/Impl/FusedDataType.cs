using MbientLab.MetaWear.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace MbientLab.MetaWear.Impl {
    internal class FusedData  : DataBase {
        private readonly bool logData;

        public FusedData(bool logData, DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                base(bridge, datatype, timestamp, bytes) {
            this.logData = logData;
        }

        public override Type[] Types => new Type[] { typeof(IData[]) };

        public override T Value<T>() {
            var type = typeof(T);

            if (type != typeof(IData[])) {
                return base.Value<T>();
            }

            var dpModule = bridge.GetModule<IDataProcessor>() as DataProcessor;
            var fuser = dpModule.activeProcessors[datatype.eventConfig[2]];

            while (!(fuser.Item2.configObj is DataProcessorConfig.FuserConfig)) {
                fuser = dpModule.activeProcessors[fuser.Item2.source.input.eventConfig[2]];
            }

            var source = fuser.Item2.source.input ?? fuser.Item2.source;
            int offset = 0;
            IData[] unwrapped = new IData[fuser.Item2.config.Length + 1];

            unwrapped[0] = source.createData(logData, bridge, bytes, Timestamp);
            offset += source.attributes.length();

            for (int i = 2; i < fuser.Item2.config.Length; i++) {
                var value = dpModule.activeProcessors[fuser.Item2.config[i]];
                // buffer state holds actual data type
                byte[] portion = new byte[value.Item1.attributes.length()];

                Array.Copy(bytes, offset, portion, 0, portion.Length);
                unwrapped[i - 1] = value.Item1.createData(logData, bridge, portion, Timestamp);

                offset += value.Item1.attributes.length();
            }

            return (T)Convert.ChangeType(unwrapped, type);
        }
    }

    class FusedDataType : DataTypeBase {
        internal FusedDataType(Module module, byte register, DataAttributes attributes) :
            base(module, register, attributes) { }

        internal FusedDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
            base(input, module, register, id, attributes) { }

        internal FusedDataType(DataTypeBase input, Module module, byte register, DataAttributes attributes) :
            base(input, module, register, attributes) { }

        public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            return new FusedDataType(input, module, register, id, attributes);
        }

        public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
            return new FusedData(logData, this, bridge, timestamp, data);
        }
    }
}