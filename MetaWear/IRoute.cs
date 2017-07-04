using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Defines how data flows from a data producer to an endpoint
    /// </summary>
    public interface IRoute {
        /// <summary>
        /// Unique value identifying the route.  
        /// <para>Use with <see cref="IMetaWearBoard.LookupRoute(uint)"/> to retrive an existing route</para>
        /// </summary>
        uint ID { get; }
        /// <summary>
        /// True if this object is still useable, discard if false
        /// </summary>
        bool Valid { get; }

        /// <summary>
        /// Assigns a subscriber to the specified data consumer (stream or logger)
        /// </summary>
        /// <param name="pos">Numerical position of the subscriber to interact with, starting at 0</param>
        /// <param name="subscriber">Handler to process the received data</param>
        /// <returns>True if function succeeded, false otherwise</returns>
        bool AttachSubscriber(int pos, Action<IData> subscriber);
        /// <summary>
        /// Reactivates the stream the subscriber is listening to, does nothing if the subscriber is handling log data
        /// </summary>
        /// <param name="pos">Numerical position of the subscriber to interact with, starting at 0</param>
        /// <returns>True if function succeed, false otherwise</returns>
        bool Resubscribe(int pos);
        /// <summary>
        /// Quiets the stream the subscriber is listening to, does nothing if the subscriber is handling log data
        /// </summary>
        /// <param name="pos">Numerical position of the subscriber to interact with, starting at 0</param>
        /// <returns>True if function succeed, false otherwise</returns>
        bool Unsubscribe(int pos);
        /// <summary>
        /// Removes the route and marks the object as invalid
        /// </summary>
        void Remove();
    }
}
