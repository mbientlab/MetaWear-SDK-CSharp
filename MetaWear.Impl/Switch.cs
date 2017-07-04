using static MbientLab.MetaWear.Impl.Module;

using System;
using MbientLab.MetaWear.Peripheral;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Switch : ModuleImplBase, ISwitch {
        private const byte STATE = 0x1;

        private ActiveDataProducer<byte> switchProducer = null;

        public IActiveDataProducer<byte> State {
            get {
                if (switchProducer == null) {
                    switchProducer = new ActiveDataProducer<byte>(new IntegralDataType(SWITCH, STATE, new DataAttributes(new byte[] { 1 }, 1, 0, false)), bridge);
                }
                return switchProducer;
            }
        }

        public Switch(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            bridge.addRegisterResponseHandler(Tuple.Create((byte)SWITCH, Util.setRead(STATE)), response => switchProducer.SetReadResult(response[2]));
        }
    }
}
