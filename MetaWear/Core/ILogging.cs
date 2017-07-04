using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Core {
    /// <summary>
    /// Types of errors encountered during a log download
    /// </summary>
    public enum LogDownloadError {
        UNKNOWN_LOG_ENTRY,
        UNHANDLED_LOG_DATA
    }
    /// <summary>
    /// Firmware feature that saves data to the on-board flash memory.
    /// <para>This module is used in conjunction with the data route's log component</para>
    /// </summary>
    public interface ILogging : IModule {
        /// <summary>
        /// Start logging data
        /// </summary>
        /// <param name="overwrite">True if older entries should be overwritten when the logger is full, defaults to false</param>
        void Start(bool overwrite = false);
        /// <summary>
        /// Stop logging data
        /// </summary>
        void Stop();

        /// <summary>
        /// Clear all stored logged data from the board.  The erase operation will not be performed until
        /// you disconnect from the board.
        /// </summary>
        void ClearEntries();

        /// <summary>
        /// Download saved data from the flash memory with periodic progress updates
        /// </summary>
        /// <param name="nUpdates">How many progress updates to send during the download</param>
        /// <param name="updateHandler">Handler to accept download notifications</param>
        /// <returns>Task that will complete when the download has finished</returns>
        Task DownloadAsync(uint nUpdates, Action<uint, uint> updateHandler);
        /// <summary>
        /// Download saved data from the flash memory with error handling but no progress updates
        /// </summary>
        /// <param name="errorHandler">Handler to process encountered errors during the download</param>
        /// <returns>Task that will complete when the download has finished</returns>
        Task DownloadAsync(Action<LogDownloadError, byte, DateTime, byte[]> errorHandler);
        /// <summary>
        /// Download saved data from the flash memory with periodic progress updates and error handling
        /// </summary>
        /// <param name="nUpdates">How many progress updates to send during the download</param>
        /// <param name="updateHandler">Handler to accept download notifications</param>
        /// <param name="errorHandler">Handler to process encountered errors during the download</param>
        /// <returns>Task that will complete when the download has finished</returns>
        Task DownloadAsync(uint nUpdates, Action<uint, uint> updateHandler, Action<LogDownloadError, byte, DateTime, byte[]> errorHandler);
        /// <summary>
        /// Download saved data from the flash memory with no progress updates nor error handling
        /// </summary>
        /// <returns>Task that will complete when the download has finished</returns>
        Task DownloadAsync();
    }
}
