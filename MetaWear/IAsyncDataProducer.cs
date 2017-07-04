namespace MbientLab.MetaWear {
    /// <summary>
    /// Data producer that emits data only when new data is available.  
    /// </summary>
    public interface IAsyncDataProducer : IDataProducer {
        /// <summary>
        /// Begin data collection
        /// </summary>
        void Start();
        /// <summary>
        /// End data collection
        /// </summary>
        void Stop();
    }
}
