namespace MbientLab.MetaWear.Peripheral {
    /// <summary>
    /// On-board push button switch
    /// </summary>
    public interface ISwitch : IModule {
        /// <summary>
        /// Data producer representing the button state
        /// </summary>
        IActiveDataProducer<byte> State { get; }
    }
}
