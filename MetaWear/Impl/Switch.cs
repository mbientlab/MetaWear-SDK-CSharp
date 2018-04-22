using static MbientLab.MetaWear.Impl.Module;

using System;
using MbientLab.MetaWear.Peripheral;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Switch : ModuleImplBase, ISwitch {
        private const byte STATE = 0x1;

        private ActiveDataProducer<byte> switchProducer = null;
        [DataMember] private DataTypeBase switchDataType;

        public IActiveDataProducer<byte> State {
            get {
                if (switchProducer == null) {
                    switchProducer = new ActiveDataProducer<byte>(switchDataType, bridge);
                }
                return switchProducer;
            }
        }

        public Switch(IModuleBoardBridge bridge) : base(bridge) {
            switchDataType = new IntegralDataType(SWITCH, STATE, new DataAttributes(new byte[] { 1 }, 1, 0, false));
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(switchDataType);
        }

        protected override void init() {
            bridge.addRegisterResponseHandler(Tuple.Create((byte)SWITCH, Util.setRead(STATE)), response => switchProducer.SetReadResult(response[2]));
        }
    }
}
