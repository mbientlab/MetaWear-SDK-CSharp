using MbientLab.MetaWear.Peripheral.NeoPixel;

namespace MbientLab.MetaWear.Peripheral {
    namespace NeoPixel {
        /// <summary>
        /// Color ordering for the NeoPixel color values
        /// </summary>
        public enum ColorOrdering {
            WS2811_RGB,
            WS2811_RBG,
            WS2811_GRB,
            WS2811_GBR
        }
        /// <summary>
        /// Operating speeds for a NeoPixel strand
        /// </summary>
        public enum StrandSpeed {
            Slow,
            Fast
        }
        /// <summary>
        /// Enumeration of rotation directions
        /// </summary>
        public enum RotationDirection {
            Towards,
            Away
        }
        /// <summary>
        /// Represents a NeoPixel strand
        /// </summary>
        public interface IStrand {
            /// <summary>
            /// Number of LEDs initialized for the strand
            /// </summary>
            int NLeds { get; }

            /// <summary>
            /// Free resources allocated by the firmware for this strand.  After calling free, this object is no longer valid and should be discarded
            /// </summary>
            void Free();
            /// <summary>
            /// Enables strand holding.  When enabled, the strand will not refresh with any LED changes until the hold 
            /// is disabled.This allows you to form complex LED patterns without having the strand refresh with partial changes.
            /// </summary>
            void Hold();
            /// <summary>
            /// Disables strand holding.  The strand will be refreshed with any LED changes programmed while the hold was active.
            /// </summary>
            void Release();
            /// <summary>
            /// Clears the LEDs in the given range
            /// </summary>
            /// <param name="start">Led index to start clearing from</param>
            /// <param name="end">Led index to clear to, exclusive</param>
            void Clear(byte start, byte end);
            /// <summary>
            /// Set and LED's rgb values
            /// </summary>
            /// <param name="index">LED index to set, from [0, nLeds - 1]</param>
            /// <param name="red">Red value, between [0, 255]</param>
            /// <param name="green">Green value, between [0, 255]</param>
            /// <param name="blue">Blue value, between [0, 255]</param>
            void SetRgb(byte index, byte red, byte green, byte blue);
            /// <summary>
            /// Rotate the LED color patterns on a strand
            /// </summary>
            /// <param name="direction">Rotation direction</param>
            /// <param name="period">Amount of time, in milliseconds (ms), between rotations</param>
            /// <param name="repetitions">Number of times to repeat the rotation, defaults to indefinite</param>
            void Rotate(RotationDirection direction, ushort period, byte repetitions = 0xff);
            /// <summary>
            /// Stops the LED rotation
            /// </summary>
            void StopRotation();
        }
    }
    /// <summary>
    /// A brand of RGB led strips by Adafruit
    /// </summary>
    public interface INeoPixel : IModule {
        /// <summary>
        /// Initialize memory on the MetaWear board for a NeoPixel strand
        /// </summary>
        /// <param name="id">Strand number (id) to initialize, can be in the range [0, 2]</param>
        /// <param name="ordering">Color ordering format</param>
        /// <param name="speed">Operating speed</param>
        /// <param name="gpioPin">GPIO pin the strand is connected to</param>
        /// <param name="nLeds">Number of LEDs to use</param>
        /// <returns>Object representing the initialized strand</returns>
        IStrand InitializeStrand(byte id, ColorOrdering ordering, StrandSpeed speed, byte gpioPin, byte nLeds);
        /// <summary>
        /// Find the object corresponding to the strand number
        /// </summary>
        /// <param name="id">Index the virtual pin is using</param>
        /// <returns>Object representing the strand, null if not found</returns>
        IStrand LookupStrand(byte id);
    }
}
