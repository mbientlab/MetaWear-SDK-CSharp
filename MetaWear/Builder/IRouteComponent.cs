using MbientLab.MetaWear.Core;
using System;

namespace MbientLab.MetaWear.Builder {
    /// <summary>
    /// 1 operand functions that operate on sensor or processor data
    /// </summary>
    public enum Function1 {
        AbsValue,
        Rms,
        Rss,
        Sqrt
    }
    /// <summary>
    /// 2 operand functions that operate on sensor or processor data
    /// </summary>
    public enum Function2 {
        Add,
        Multiply,
        Divide,
        Modulus,
        Exponent,
        LeftShift,        
        RightShift,        
        Subtract,
        Constant
    }
    /// <summary>
    /// Operation modes for the passthrough limiter
    /// </summary>
    public enum Passthrough {
        All,
        Conditional,
        Count
    }
    /// <summary>
    /// Output modes for the comparison filter, only supported by firmware v1.2.3 or higher
    /// </summary>
    public enum ComparisonOutput {
        /// <summary>
        /// Input value is returned when the comparison is satisfied
        /// </summary>
        Absolute,
        /// <summary>
        /// The reference value that satisfies the comparison is returned, no output if none match
        /// </summary>
        PassFail,
        /// <summary>
        /// The index (0 based) of the value that satisfies the comparison is returned, n if none match
        /// </summary>
        Reference,
        /// <summary>
        /// 0 if comparison failed, 1 if it passed
        /// </summary>
        Zone
    }
    /// <summary>
    /// Supported comparison operations
    /// </summary>
    public enum Comparison {
        Eq,
        Neq,
        Lt,
        Lte,
        Gt,
        Gte
    }
    /// <summary>
    /// Output types of the pulse finder
    /// </summary>
    public enum Pulse {
        /// <summary>
        /// Returns the number of samples in the pulse
        /// </summary>
        Width,
        /// <summary>
        /// Returns a running sum of all samples in the pulse
        /// </summary>
        Area,
        /// <summary>
        /// Returns the highest sample value in the pulse
        /// </summary>
        Peak,
        /// <summary>
        /// Returns 0x1 as soon as a pulse is detected
        /// </summary>
        OnDetect
    }
    /// <summary>
    /// Output modes for the threshold filter
    /// </summary>
    public enum Threshold {
        /// <summary>
        /// Return the data as is
        /// </summary>
        Absolute,
        /// <summary>
        /// 1 if the data exceeded the threshold, -1 if below
        /// </summary>
        Binary
    }
    /// <summary>
    /// Output modes for the differential filter
    /// </summary>
    public enum Differential {
        /// <summary>
        /// Return the data as is
        /// </summary>
        Absolute,
        /// <summary>
        /// Return the difference between the value and its reference point
        /// </summary>
        Differential,
        /// <summary>
        /// 1 if the difference is positive, -1 if negative
        /// </summary>
        Binary
    }
    /// <summary>
    /// Types of information the accounter processor can append to the data
    /// </summary>
    public enum AccountType {
        /// <summary>
        /// Append a looping counter to all data.  This counter is accessed by calling <see cref="IData.Extra{T}"/> 
        /// with the <code>uint</code> type.
        /// </summary>
        Count,
        /// <summary>
        /// Extra information used to calculate actual timestamps for streamed data
        /// </summary>
        Time
    }

    /// <summary>
    /// Component in a route definition
    /// </summary>
    public interface IRouteComponent {
        /// <summary>
        /// Creates a branch in the route that direct the input data to different end points
        /// </summary>
        /// <returns>Component for building a multicast branch</returns>
        IRouteMulticast Multicast();
        /// <summary>
        /// Signals the creation of a new multicast branch
        /// </summary>
        /// <returns>Component from the most recent multicast component</returns>
        IRouteComponent To();

        /// <summary>
        /// Separates multi-component data into its individual values
        /// </summary>
        /// <returns>Component for building routes for component data values</returns>
        IRouteSplit Split();
        /// <summary>
        /// Gets a specific component value from the split data value
        /// </summary>
        /// <param name="i">Position in the split values array to return</param>
        /// <returns>Object representing the component value</returns>
        IRouteComponent Index(int i);

        /// <summary>
        /// Streams the input data to the local device.  This component is represented by the <see cref="ISubscriber"/> interface.
        /// </summary>
        /// <param name="handler">Handler to process the received data</param>
        /// <returns>Calling object</returns>
        IRouteComponent Stream(Action<IData> handler);
        /// <summary>
        /// Variant of <see cref="Stream(Action{IData})"/> that enables a data stream but does not yet assign a data handler.  
        /// The handler can be later attached with <see cref="ISubscriber.Attach(Action{IData})"/>
        /// </summary>
        /// <returns>Calling object</returns>
        IRouteComponent Stream();
        /// <summary>
        /// Records the input data to the on-board logger, retrieved later when a log download is started.  This component is represented 
        /// by the <see cref="ISubscriber"/> interface.
        /// </summary>
        /// <param name="handler">Handler to process the received data</param>
        /// <returns>Calling object</returns>
        IRouteComponent Log(Action<IData> handler);
        /// <summary>
        /// Variant of <see cref="Log(Action{IData})"/> that sets up the logger but does not yet assign a data handler.  
        /// The handler can be later attached with <see cref="ISubscriber.Attach(Action{IData})"/>
        /// </summary>
        /// <returns></returns>
        IRouteComponent Log();

        /// <summary>
        /// Programs the board to react in response to data being created by the most resent sensor or processor
        /// </summary>
        /// <param name="action">On-board action to execute</param>
        /// <returns>Calling object</returns>
        IRouteComponent React(Action<IDataToken> action);

        /// <summary>
        /// Assigns a user-defined name identifying a processor, producer, or subscriber.  The name can be used to create feedback and feedforward loops, 
        /// or refere to subscribers by name rather than index.
        /// <para>Processors and subscribers are retrieved by name with the <see cref="IDataProcessor"/> interface and <see cref="IRoute.LookupSubscriber(string)"/> method respectively.</para>
        /// </summary>
        /// <param name="name">Assigned unique name to the most recent data producer (sensor or data processor)</param>
        /// <returns>Calling object</returns>
        IRouteComponent Name(string name);

        /// <summary>
        /// Accumulates a running sum of all data samples passing through this component and outputs the current tally
        /// </summary>
        /// <returns>Component representing the accumulated sum</returns>
        IRouteComponent Accumulate();
        /// <summary>
        /// Counts the number of data samples that have passed through this component and outputs the current count
        /// </summary>
        /// <returns>Component representing the counter output</returns>
        IRouteComponent Count();

        /// <summary>
        /// Stores the input data in memory which can be extracted by reading the buffer state.  As this buffer does not have an output, 
        /// the route cannot continue so it must either end or control is passed back to the most recent split or multicast
        /// </summary>
        /// <returns>Object for continuing the route</returns>
        IRouteBranchEnd Buffer();

        /// <summary>
        /// Stops data from passing until at least N samples have been collected.
        /// </summary>
        /// <param name="samples">Number of samples to collect</param>
        /// <returns>Component representing the delayed output</returns>
        IRouteComponent Delay(byte samples);

        /// <summary>
        /// Computes a moving average over the previous N samples.  This component will not output data until the first average i.e.
        /// until N samples have been received.
        /// </summary>
        /// <param name="samples">Number of samples to average over</param>
        /// <returns>Component representing the running averager</returns>
        [Obsolete("Deprecated in v0.2, use LowPass component instead")]
        IRouteComponent Average(byte samples);
        /// <summary>
        /// Applies a high pass filter over the input data, available on firmware v1.3.4 and later.
        /// </summary>
        /// <param name="samples">Number of previous data samples to compare against</param>
        /// <returns>Component representing the high pass output</returns>
        IRouteComponent HighPass(byte samples);
        /// <summary>
        /// Applies a low pass filter over the input data, available on firmware v1.3.4 and later.  
        /// <para>This componenet replaces the <see cref="Average(byte)"/> component.</para>
        /// </summary>
        /// <param name="samples">Number of previous data samples to compare against</param>
        /// <returns>Component representing the low pass output</returns>
        IRouteComponent LowPass(byte samples);

        /// <summary>
        /// Remove data from the route that does not satisfy the comparison
        /// </summary>
        /// <param name="op">Comparison operation to perform</param>
        /// <param name="references">Reference values to compare against, can be multiple values if the board is on firmware v1.2.3 or later</param>
        /// <returns>Component representing the filter output</returns>
        IRouteComponent Filter(Comparison op, params float[] references);
        /// <summary>
        /// Variant of <see cref="Filter(Comparison, float[])"/> where the filter output provides
        /// additional details about the comparison.  This variant component is only supported starting with
        /// firmware v1.2.5.  <b>Note that if <see cref="ComparisonOutput.Reference"/>
        /// or <see cref="ComparisonOutput.Zone"/> is used, component will instead function as a <code>map</code></b>
        /// </summary>
        /// <param name="op">Comparison operation to perform</param>
        /// <param name="output">Output type the filter should produce</param>
        /// <param name="references">Reference values to compare against, can be multiple values if the board is on firmware v1.2.3 or later</param>
        /// <returns>Component representing the filter output</returns>
        IRouteComponent Filter(Comparison op, ComparisonOutput output, params float[] references);
        /// <summary>
        /// Variant of the <see cref="Filter(Comparison, float[])"/> function where the reference values are outputs
        /// from other sensors or processors
        /// </summary>
        /// <param name="op">Comparison operation to perform</param>
        /// <param name="names">Names identifying which sensor or processor data to use as the reference value when new values are produced</param>
        /// <returns>Component representing the filter output</returns>
        IRouteComponent Filter(Comparison op, params string[] names);
        /// <summary>
        /// Variant of <see cref="Filter(Comparison, ComparisonOutput, float[])"/> where reference values are outputs
        /// from other sensors or processors.
        /// </summary>
        /// <param name="op">Comparison operation to perform</param>
        /// <param name="output">Output type of the filter</param>
        /// <param name="names">Names identifying which sensor or processor data to use as the reference value when new values are produced</param>
        /// <returns>Component representing the filter output</returns>
        IRouteComponent Filter(Comparison op, ComparisonOutput output, params string[] names);

        /// <summary>
        /// Scans the input data for a pulse.  When one is detected, output a summary of the scanned data
        /// </summary>
        /// <param name="pulse">Type of summary data to output</param>
        /// <param name="threshold">Value the sensor data must exceed for a valid pulse</param>
        /// <param name="samples">Minimum number of samples that must be above the threshold for a valid pulse</param>
        /// <returns>Component representing the output of the pulse finder</returns>
        IRouteComponent Find(Pulse pulse, float threshold, ushort samples);
        /// <summary>
        /// Scans the input data for values that cross a boundary, either falling below or rising above
        /// </summary>
        /// <param name="threshold">Type of summary data to output</param>
        /// <param name="boundary">Threshold boundary the data must cross</param>
        /// <returns>Component representing the threshold filter output</returns>
        IRouteComponent Find(Threshold threshold, float boundary);
        /// <summary>
        /// Variant of <see cref="Find(Threshold, float)"/> with a configurable hysteresis value for data
        /// that frequently oscillates around the threshold boundary
        /// </summary>
        /// <param name="threshold">Type of summary data to output</param>
        /// <param name="boundary">Threshold boundary the data must cross</param>
        /// <param name="hysteresis">Minimum distance between the boundary and value that indicates a successful crossing</param>
        /// <returns>Component representing the threshold filter output</returns>
        IRouteComponent Find(Threshold threshold, float boundary, float hysteresis);
        /// <summary>
        /// Scans the input data for sequential data that is a minimum distance away 
        /// </summary>
        /// <param name="differential"></param>
        /// <param name="distance"></param>
        /// <returns>Component representing the differential filter output</returns>
        IRouteComponent Find(Differential differential, float distance);

        /// <summary>
        /// Only allow data through under certain user controlled conditions
        /// </summary>
        /// <param name="type">Passthrough operation type</param>
        /// <param name="value">Initial value to set the passthrough limiter to</param>
        /// <returns>Component representing the limiter output</returns>
        IRouteComponent Limit(Passthrough type, ushort value);
        /// <summary>
        /// Reduce the amount of data allowed through such that the output data rate matches the delay
        /// </summary>
        /// <param name="period">How often to allow data through, in milliseconds (ms)</param>
        /// <returns>Component representing the output of the limiter</returns>
        IRouteComponent Limit(uint period);

        /// <summary>
        /// Apply a 1 input function to all of the input data
        /// </summary>
        /// <param name="fn">Function to use</param>
        /// <returns>Component representing the mapper output</returns>
        IRouteComponent Map(Function1 fn);
        /// <summary>
        /// Apply a 2 input function to all of the input data
        /// </summary>
        /// <param name="fn">Function to use</param>
        /// <param name="rhs">Second operand for the function</param>
        /// <returns>Component representing the mapper output</returns>
        IRouteComponent Map(Function2 fn, float rhs);
        /// <summary>
        /// Variant of <see cref="Map(Function2, float)"/> where the rhs value is the output of another data producer
        /// </summary>
        /// <param name="fn">Function to apply to the input data</param>
        /// <param name="names">Names identifying which producer to feed into the mapper</param>
        /// <returns>Component representing the mapper output</returns>
        IRouteComponent Map(Function2 fn, params string[] names);

        /// <summary>
        /// Variant of <see cref="Account(AccountType)"/> that defaults to recalculating timestamps
        /// </summary>
        /// <returns>Component representing the accounter output</returns>
        IRouteComponent Account();
        /// <summary>
        /// Add additional information to the payload to assist in checking if streamed data is lost
        /// </summary>
        /// <param name="type">Type of information to append to the data</param>
        /// <returns>Component representing the accounter output</returns>
        IRouteComponent Account(AccountType type);
        /// <summary>
        /// Packs multiple input values into 1 BTLE packet.  Used to reduce the number of packets broadcasted over the link.
        /// </summary>
        /// <param name="count">Number of input values to pack</param>
        /// <returns>Component representing the accounter output</returns>
        IRouteComponent Pack(byte count);

        /// <summary>
        /// Combines data from multiple sources into 1 packet, available on firmware v1.4.4+.  
        /// The additional data you want to combine must first be stored into a named buffer.
        /// </summary>
        /// <param name="bufferNames">Named buffer components holding the extra data to combine</param>
        /// <returns>Component representing the fuser output</returns>
        IRouteComponent Fuse(params string[] bufferNames);
    }
}
