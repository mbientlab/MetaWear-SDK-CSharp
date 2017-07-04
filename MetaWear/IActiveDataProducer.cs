using System.Threading.Tasks;

namespace MbientLab.MetaWear {
    /// <summary>
    /// A type DataProducer that is always producing data and is not user controlled.
    /// </summary>
    /// <typeparam name="T">Type that the raw value should be interpreted as</typeparam>
    public interface IActiveDataProducer<T> : IDataProducer {
        /// <summary>
        /// Reads the current value of the producer
        /// </summary>
        /// <returns>Current value</returns>
        /// <exception cref="System.TimeoutException">If task takes longer than 250ms to complete</exception>
        Task<T> ReadAsync();
    }
}
