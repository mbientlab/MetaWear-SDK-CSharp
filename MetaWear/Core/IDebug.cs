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
        /// <returns>Null when connection is lost</returns>
        Task ResetAsync();
        /// <summary>
        /// Commands the board to reset after performing garbage collection.  Use this function in lieu of
        /// <see cref="ResetAsync"/> to reset the board after erasing macros or log data.
        /// </summary>
        void ResetAfterGc();

        /// <summary>
        /// Commands the board to terminate the BLE link.
        /// <para>This task will be immediately cancelled if called within the context of a reaction</para>
        /// </summary>
        /// <returns>Null when the connection is lost</returns>
        Task DisconnectAsync();
        /// <summary>
        /// Restarts the board in MetaBoot mode which enables firmware updates.
        /// <para>This task will be immediately cancelled if called within the context of a reaction</para>
        /// </summary>
        /// <returns>Null when the connection is lost</returns>
        Task JumpToBootloaderAsync();
        
        /// <summary>
        /// Places the board in a powered down state after the next reset.  
        /// <para>When in power save mode, press the switch to wake the board up.</para>
        /// </summary>
        /// <returns>True if feature is supported, false if powersave cannot be enabled</returns>
        bool EnablePowerSave();

        /// <summary>
        /// Writes a signed int that persists until a reset, can be later retrieved with <see cref="ReadTmpValueAsync"/>
        /// </summary>
        /// <param name="value">Value to write</param>
        void WriteTmpValue(int value);
        /// <summary>
        /// Reads the temp value written by <see cref="WriteTmpValue(int)"/>
        /// </summary>
        /// <returns>Temp value read from the device</returns>
        Task<int> ReadTmpValueAsync();
    }
}
