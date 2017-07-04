namespace MbientLab.MetaWear.Builder{
    /// <summary>
    /// Route element for enforcing that users call <see cref="To"/> or <see cref="Index(int)"/> immediately after a terminating branch
    /// </summary>
    public interface IRouteBranchEnd {
        /// <summary>
        /// Signals the creation of a new multicast branch
        /// </summary>
        /// <returns>Component representing the most recent multicast branch</returns>
        IRouteComponent To();
        /// <summary>
        /// Gets a specific component value from the split data value
        /// </summary>
        /// <param name="i">Position in the split values array to return</param>
        /// <returns>Object representing the component value</returns>
        IRouteComponent Index(int i);
    }
}
