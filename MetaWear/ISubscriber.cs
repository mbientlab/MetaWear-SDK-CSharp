using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Represents a <code>Stream</code> or <code>Log</code> component added in a route builder
    /// </summary>
    public interface ISubscriber {
        /// <summary>
        /// String identifying the data producer chain the subscriber is receiving data from.
        /// <para>
        /// This value can be matched with the <see cref="IAnonymousRoute.Identifier"/> property if syncing 
        /// logged data with the <see cref="IAnonymousRoute"/> interface.
        /// </para>
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Assigns a handler for the received data
        /// </summary>
        /// <param name="handler">Handler to process the received data</param>
        void Attach(Action<IData> handler);
        /// <summary>
        /// Activates the subscriber and assigns a handler for the received data
        /// </summary>
        /// <param name="handler">Handler to process the received data, null if existing handler should be used</param>
        void Listen(Action<IData> handler = null);
        /// <summary>
        /// Quiets the streamed data the subscriber is listening to, does nothing if the subcriber is listening to logged data
        /// </summary>
        void Quiet();
    }
}
