using MbientLab.MetaWear.Core;
using System;
using System.Collections;
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
        /// Gets the string representation of the board's model, null if unable to determine
        /// </summary>
        String ModelString { get; }
        /// <summary>
        /// Called when the connection is unexpectedly lost i.e. not requested by the API
        /// </summary>
        Action OnUnexpectedDisconnect { get; set; }
        /// <summary>
        /// How long the API should wait (in milliseconds) before a required response is received
        /// </summary>
        int TimeForResponse { set; }
        /// <summary>
        /// True if the board is currently connected to the host device
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connect to the remote device and perpares the internal API state to communicate with the modules.
        /// </summary>
        /// <param name="timeout">How long to wait (in milliseconds) for initialization to complete</param>
        /// <returns>Null once the SDK is initialized</returns>
        /// <exception cref="TimeoutException">If initialization takes too long, timeout can be increased with the <see cref="TimeForResponse"/> property</exception>
        /// <exception cref="InvalidOperationException">If the host device failed to establish a connection</exception>
        Task InitializeAsync();
        /// <summary>
        /// Disconnects from the MetaWear device
        /// <para>This method is only meaningful for non Windows 10 applications due to quirks with Win10's BLE stack.  For consistent behavior across all platforms, 
        /// use the <see cref="IDebug.DisconnectAsync"/> instead to terminate the connection</para>
        /// </summary>
        /// <returns>Null when the connection is closed</returns>
        Task DisconnectAsync();

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
        /// Queries all info registers.  If the task times out, you can run the task again using the partially
        /// completed result from the previous execution so the function does not need to query all modules again.
        /// </summary>
        /// <param name="partial">Result of previously queried module info results, set to null to query all modules</param>
        /// <returns>Module information contained in a string indexed dictionary</returns>
        /// <exception cref="TaskTimeoutException">If the module responses take too long, partial result is included with this exception</exception>
        Task<IDictionary> GetModuleInfoAsync(IDictionary partial);

        /// <summary>
        /// Serialize object state and write the state to the local disk
        /// </summary>
        /// <returns>Null when the object state is saved</returns>
        Task SerializeAsync();
        /// <summary>
        /// Restore serialized state from the local disk if available
        /// </summary>
        /// <returns>Null when the object state is restored</returns>
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