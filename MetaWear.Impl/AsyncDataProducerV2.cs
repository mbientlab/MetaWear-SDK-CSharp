namespace MbientLab.MetaWear.Impl {
    class AsyncDataProducerV2 : AsyncDataProducer {
        protected byte mask;

        internal AsyncDataProducerV2(byte mask, DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : 
            this(dataTypeBase.eventConfig[1], mask, dataTypeBase, bridge) { }

        internal AsyncDataProducerV2(byte register, byte mask, DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(register, dataTypeBase, bridge) {
            this.mask = mask;
        }

        public override void Start() {
            bridge.sendCommand(new byte[] { dataTypeBase.eventConfig[0], enableRegister, mask, 0x0 });
        }

        public override void Stop() {
            bridge.sendCommand(new byte[] { dataTypeBase.eventConfig[0], enableRegister, 0x00, mask});
        }
    }
}
