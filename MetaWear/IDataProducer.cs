using MbientLab.MetaWear.Builder;

using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear {
    /// <summary>
    /// A component that creates data, such as firmware features (battery level reporting) or sensors
    /// </summary>
    public interface IDataProducer {
        /// <summary>
        /// Adds a data route to the producer
        /// </summary>
        /// <param name="builder">Builder object to construct the route</param>
        /// <returns>Object representing the created route</returns>
        /// <exception cref="TimeoutException">If creating reaction, loggers, or data processors takes too long</exception>
        /// <exception cref="IllegalRouteOperationException">If an invalid combination of route components are used</exception>
        Task<IRoute> AddRouteAsync(Action<IRouteComponent> builder);
    }
}
