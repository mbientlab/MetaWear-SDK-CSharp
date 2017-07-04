using System.Threading.Tasks;

namespace MbientLab.MetaWear.Core {
    /// <summary>
    /// Firmware feature that saves MetaWear commands to the on-board flash memory
    /// </summary>
    public interface IMacro : IModule {
        /// <summary>
        /// Starts macro recording.  Every MetaWear command issued will be recorded to the flash memory.
        /// </summary>
        /// <param name="execOnBoot">True if the commands should be executed when the board powers on, defaults to true</param>
        void StartRecord(bool execOnBoot = true);
        /// <summary>
        /// Ends macro recording
        /// </summary>
        /// <returns>Task containing the id of the recorded task</returns>
        Task<byte> EndRecordAsync();
        /// <summary>
        /// Execute the commands corresponding to the macro ID
        /// </summary>
        /// <param name="id">Numerical ID of the macro to execute</param>
        void Execute(byte id);
        /// <summary>
        /// Remove all macros on the flash memory.  The erase operation will not be performed until
        /// you disconnect from the board.If you wish to reset the board after the erase operation,
        /// use the <see cref="IDebug.ResetAfterGc"/> method.
        /// </summary>
        void EraseAll();
    }
}
