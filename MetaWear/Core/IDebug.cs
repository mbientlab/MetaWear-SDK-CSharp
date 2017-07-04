using System.Threading.Tasks;

namespace MbientLab.MetaWear.Core {
    /// <summary>
    /// Auxiliary functions, for advanced use only
    /// </summary>
    public interface IDebug : IModule {
        /// <summary>
        /// Issues a firmware reset command to the board.  
        /// <para>This task will be immediately cancelled if called within the context of a reaction</para>
        /// </summary>
        /// <returns>True when connection is lost.</returns>
        Task<bool> ResetAsync();
        /// <summary>
        /// Commands the board to terminate the BLE link.
        /// <para>This task will be immediately cancelled if called within the context of a reaction</para>
        /// </summary>
        /// <returns>True when the connection is lost</returns>
        Task<bool> DisconnectAsync();
        /// <summary>
        /// Restarts the board in MetaBoot mode which enables firmware updates.
        /// <para>This task will be immediately cancelled if called within the context of a reaction</para>
        /// </summary>
        /// <returns>True when the connection is lost</returns>
        Task<bool> JumpToBootloaderAsync();
        /// <summary>
        /// Commands the board to reset after performing garbage collection.  Use this function in lieu of
        /// <see cref="ResetAsync"/> to reset the board after erasing macros or log data.
        /// </summary>
        void ResetAfterGc();
    }
}
