namespace MbientLab.MetaWear.Builder {
    /// <summary>
    /// RouteComponent for enforcing that users call <see cref="Index(int)"/> immediately after splitting data
    /// </summary>
    public interface IRouteSplit {
        /// <summary>
        /// Gets a specific component value from the split data value
        /// </summary>
        /// <param name="i">Position in the split values array to return</param>
        /// <returns>Object representing the component value</returns>
        IRouteComponent Index(int i);
    }
}
