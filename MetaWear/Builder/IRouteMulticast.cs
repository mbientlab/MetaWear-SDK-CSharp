namespace MbientLab.MetaWear.Builder {
    /// <summary>
    /// Route element for enforcing that users call <see cref="To"/> immediately after declaring a multicast
    /// </summary>
    public interface IRouteMulticast {
        /// <summary>
        /// Signals the creation of a new multicast branch
        /// </summary>
        /// <returns>Component representing the most recent multicast branch</returns>
        IRouteComponent To();
    }
}
