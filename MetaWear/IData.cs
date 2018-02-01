using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// A sample of sensor data
    /// </summary>
    public interface IData {
        /// <summary>
        /// Types that can be used to extract the values from the IData object
        /// </summary>
        Type[] Types { get; }
        /// <summary>
        /// Raw byte representation of the data value
        /// </summary>
        byte[] Bytes { get; }
        /// <summary>
        /// Time of when the data was received (streaming) or created (logging)
        /// </summary>
        DateTime Timestamp { get; }
        /// <summary>
        /// String representation of the timestamp in the format <code>YYYY-MM-DDTHH:MM:SS.LLL</code>.  The timezone
        /// of the string will be the local device's current timezone.
        /// </summary>
        String FormattedTimestamp { get; }
        /// <summary>
        /// LSB to units ratio.  Only used if developer is manually type casting the returned byte array from
        /// the <see cref="Bytes"/> property
        /// </summary>
        float Scale { get; }

        /// <summary>
        /// Extra information attached to this data sample
        /// </summary>
        /// <typeparam name="T">Type to cast the return value as</typeparam>
        /// <returns>Extra information as the specified type</returns>
        T Extra<T>();
        /// <summary>
        /// Converts the data bytes to a usable data type
        /// </summary>
        /// <typeparam name="T">Type to cast the return value as</typeparam>
        /// <returns>Data value as the specified type</returns>
        T Value<T>();
    }
}
