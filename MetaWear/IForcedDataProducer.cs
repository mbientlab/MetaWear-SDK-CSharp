namespace MbientLab.MetaWear {
    /// <summary>
    /// A data producer that only emits data when a <see cref="Read"/> command is issued.
    /// <para>Read commands can be scheduled on the MetaWear using 
    /// <see cref="IMetaWearBoard.ScheduleAsync(uint, bool, System.Action)"/> to avoid having to 
    /// repeatedly send the command from the local device.</para>
    /// </summary>
    public interface IForcedDataProducer : IDataProducer {
        /// <summary>
        /// Sends a read command to the producer
        /// </summary>
        void Read();
    }
}
