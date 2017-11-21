using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Pared down variant of the <see cref="IRoute"/> interface that only has one subscriber.
    /// </summary>
    /// <para>
    /// This interface is used to retrieve logged data from a board that was not programmed 
    /// by the current device.
    /// </para>
    public interface IAnonymousRoute {
        /// <summary>
        /// String identifying the data producer chain the route is receiving data from
        /// </summary>
        String Identifier { get; }
        /// <summary>
        /// Subscribe to the data produced by this chain
        /// </summary>
        /// <param name="subscriber">Subscriber implementation to handle the received data</param>
        void Subscribe(Action<IData> subscriber);
    }
}
