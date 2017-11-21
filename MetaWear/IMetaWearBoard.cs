using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Sensor, peripheral, or firmware feature
    /// </summary>
    public interface IModule { }

    /// <summary>
    /// A task comprising of MetaWear commands programmed to periodically run on-board
    /// </summary>
    public interface IScheduledTask {
        /// <summary>
        /// True if this object is still useable, discard if false
        /// </summary>
        bool Valid { get; }
        /// <summary>
        /// Numerical ID representing this object, used with <see cref="IMetaWearBoard.LookupScheduledTask(byte)"/>
        /// </summary>
        byte ID { get; }

        /// <summary>
        /// Start task execution
        /// </summary>
        void Start();
        /// <summary>
        /// Stop task execution
        /// </summary>
        void Stop();
        /// <summary>
        /// Removes this task from the board
        /// </summary>
        void Remove();
    }

    /// <summary>
    /// Software representation of all MbientLab sensor boards.
    /// <para>Call <see cref="InitializeAsync"/> before using any other functions or reading the properties.</para>
    /// </summary>
    public interface IMetaWearBoard {
        /// <summary>
        /// Unique MAC address identifying the board
        /// </summary>
        string MacAddress { get; }
        /// <summary>
        /// True if the board is in MetaBoot (bootloader) mode.  
        /// <para>If it is, you will not be able to interact with the board except to update firmware</para>
        /// </summary>
        bool InMetaBootMode { get; }
        /// <summary>
        /// Gets the model of the connected board, returns null if unable to determine
        /// </summary>
        Model? Model { get; }
        /// <summary>
        /// Called when the connection is unexpectedly lost i.e. not requested by the API
        /// </summary>
        Action OnUnexpectedDisconnect { get; set; }
        /// <summary>
        /// How long the API should wait (in milliseconds) before a required response is received.  
        /// <para>This setting only affects creating timers, loggers, data processors, and macros.</para>
        /// </summary>
        int TimeForResponse { set; }

        /// <summary>
        /// Initialize the API's internal state and establish a connection to the board
        /// </summary>
        /// <param name="timeout">How long to wait (in milliseconds) for initialization to completed</param>
        /// <returns>Null</returns>
        /// <exception cref="TimeoutException">Initialization does not complete within the allotted time</exception>
        Task InitializeAsync(int timeout = 10000);

        /// <summary>
        /// Reads supported characteristics from the Device Information service
        /// </summary>
        /// <returns>Object encapsulating the device information</returns>
        Task<DeviceInformation> ReadDeviceInformationAsync();
        /// <summary>
        /// Reads the battery level characteristic
        /// </summary>
        /// <returns>Value of the characteristic</returns>
        Task<byte> ReadBatteryLevelAsync();
        /// <summary>
        /// Retrieves a reference to the requested module if supported.
        /// <para>The API must be initialized before calling this function and it cannot be used if the board 
        /// is in MetaBoot mode.</para>
        /// </summary>
        /// <typeparam name="T">Interface derived from <see cref="IModule"/> to lookup</typeparam>
        /// <returns>Reference to the requested module, null if the board is not connected, module not supported, or board is in MetaBoot mode</returns>
        T GetModule<T>() where T : class, IModule;

        /// <summary>
        /// Serialize object state and write the state to the local disk
        /// </summary>
        /// <returns>Null</returns>
        Task SerializeAsync();
        /// <summary>
        /// Restore serialized state from the local disk if available
        /// </summary>
        /// <returns>Null</returns>
        Task DeserializeAsync();

        /// <summary>
        /// Schedule a task to be indefinitely executed on-board at fixed intervals for a specific number of repetitions
        /// </summary>
        /// <param name="period">How often to execute the task, in milliseconds</param>
        /// <param name="repititions">How many times to execute the task</param>
        /// <param name="delay">True if first execution should be delayed by one period</param>
        /// <param name="commands">MetaWear commands comprising the task</param>
        /// <returns><see cref="IScheduledTask"/> object representing the newly scheduled task</returns>
        /// <exception cref="TimeoutException">If creating the timer or programming the commands timed out</exception>
        Task<IScheduledTask> ScheduleAsync(uint period, ushort repititions, bool delay, Action commands);
        /// <summary>
        /// Schedule a task to be indefinitely executed on-board at fixed intervals indefinitely
        /// </summary>
        /// <param name="period">How often to execute the task, in milliseconds</param>
        /// <param name="delay">True if first execution should be delayed by one period</param>
        /// <param name="commands">MetaWear commands comprising the task</param>
        /// <returns><see cref="IScheduledTask"/> object representing the newly scheduled task</returns>
        /// <exception cref="TimeoutException">If creating the timer or programming the commands timed out</exception>
        Task<IScheduledTask> ScheduleAsync(uint period, bool delay, Action commands);
        /// <summary>
        /// Retrieves a scheduled task
        /// </summary>
        /// <param name="id">Numerical ID to lookup</param>
        /// <returns><see cref="IScheduledTask"/> corresonding to the specified ID, null if non can be found</returns>
        IScheduledTask LookupScheduledTask(byte id);

        /// <summary>
        /// Reads the current state of the board and creates anonymous routes based on what data is being logged
        /// </summary>
        /// <returns>List of created anonymous route objects</returns>
        Task<IList<IAnonymousRoute>> CreateAnonymousRoutesAsync();
        /// <summary>
        /// Retrieves an observer
        /// </summary>
        /// <param name="id">Numerical ID to look up</param>
        /// <returns><see cref="IObserver"/> corresponding to the specified ID, null if none can be found</returns>
        IObserver LookupObserver(uint id);
        /// <summary>
        /// Retrieves a route
        /// </summary>
        /// <param name="id">Numerical ID to look up</param>
        /// <returns><see cref="IRoute"/> corresponding to the specified ID, null if none can be found</returns>
        IRoute LookupRoute(uint id);

        /// <summary>
        /// Removes all routes and resources allocated on the board (observers, data processors, timers, and loggers)
        /// </summary>
        void TearDown();
    }
}