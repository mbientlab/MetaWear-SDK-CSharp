namespace MbientLab.MetaWear {
    /// <summary>
    /// Monitors an on-board event and executes the corresponding MetaWear commands when its event is fired
    /// </summary>
    public interface IObserver {
        /// <summary>
        /// True if this object is still useable, discard if false
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Unique value identifying the observer.  
        /// <para>Use with <see cref="IMetaWearBoard.LookupObserver(uint)"/> to retrive an existing observer</para>
        /// </summary>
        uint ID { get; }
        /// <summary>
        /// Removes the observer from the board marks the object as invalid
        /// </summary>
        void Remove();
    }
}
