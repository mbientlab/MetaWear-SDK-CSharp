using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(DataAttributes))]
    [KnownType(typeof(DataTypeBase))]
    [DataContract(IsReference = true)]
    abstract class DataTypeBase : IDataToken {
        internal const byte NO_ID = 0xff;

        [DataMember] internal readonly byte[] eventConfig;
        [DataMember] internal readonly DataAttributes attributes;
        [DataMember] internal readonly DataTypeBase input;
        [DataMember] internal readonly DataTypeBase[] components;

        internal DataTypeBase(Module module, byte register, DataAttributes attributes) :
            this(null, module, register, NO_ID, attributes) { }

        internal DataTypeBase(Module module, byte register, byte id, DataAttributes attributes) :
            this(null, module, register, id, attributes) { }

        internal DataTypeBase(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            eventConfig = new byte[] { (byte) module, register, id };
            this.attributes = attributes;
            this.input = input;
            components = createSplits();

            markSilent();
        }

        public Tuple<byte, byte, byte> eventConfigAsTuple() {
            return Tuple.Create(eventConfig[0], eventConfig[1], eventConfig[2]);
        }

        public void markSilent() {
            if ((eventConfig[1] & 0x80) == 0x80) {
                eventConfig[1] |= 0x40;
            }
        }

        public void markLive() {
            if ((eventConfig[1] & 0x80) == 0x80) {
                eventConfig[1] &= (~0x40 & 0xff);
            }
        }

        public byte[] createReadStateCmd() {
            return eventConfig[2] == NO_ID ? new byte[] { eventConfig[0], Util.setRead(eventConfig[1]) } : 
                new byte[] { eventConfig[0], Util.setRead(eventConfig[1]), eventConfig[2] };
        }

        public void read(IModuleBoardBridge bridge) {
            if (eventConfig[2] == NO_ID) {
                bridge.sendCommand(new byte[] { eventConfig[0], eventConfig[1] });
            } else {
                bridge.sendCommand(eventConfig);
            }
        }

        public void read(IModuleBoardBridge bridge, byte[] parameters) {
            int length = (eventConfig[2] == NO_ID ? 2 : 3);
            byte[] cmd = new byte[parameters.Length + length];
            Array.Copy(eventConfig, 0, cmd, 0, length);
            Array.Copy(parameters, 0, cmd, length, parameters.Length);

            bridge.sendCommand(cmd);
        }

        public virtual float scale(IModuleBoardBridge bridge) {
            return (input == null) ? 1f : input.scale(bridge);
        }

        public abstract DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes);
        public DataTypeBase dataProcessorCopy(DataTypeBase input, DataAttributes attributes) {
            return copy(input, DATA_PROCESSOR, DataProcessor.NOTIFY, NO_ID, attributes);
        }
        public DataTypeBase dataProcessorStateCopy(DataTypeBase input, DataAttributes attributes) {
            return copy(input, DATA_PROCESSOR, Util.setRead(DataProcessor.STATE), NO_ID, attributes);
        }

        public abstract IData createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp);
        protected virtual DataTypeBase[] createSplits() {
            return null;
        }
    }
}
