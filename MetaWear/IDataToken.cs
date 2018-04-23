namespace MbientLab.MetaWear {
    /// <summary>
    /// Dummy class representing a sample of sensor data within the context of programming
    /// advanced reactions in a data route.
    /// </summary>
    public interface IDataToken {
        /// <summary>
        /// Creates a <code>DataToken</code> copy that represents a portion of the original data
        /// </summary>
        /// <param name="offset">Byte to start copying from</param>
        /// <param name="length">Number of bytes to copy</param>
        /// <returns></returns>
        IDataToken Slice(byte offset, byte length);
    }
}
