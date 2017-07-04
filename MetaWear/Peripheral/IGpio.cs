using MbientLab.MetaWear.Peripheral.Gpio;
using System.Collections.Generic;

namespace MbientLab.MetaWear.Peripheral {
    namespace Gpio {
        /// <summary>
        /// Pin change types
        /// </summary>
        public enum PinChangeType {
            /// <summary>
            /// Notify on the rising edge during a change
            /// </summary>
            Rising,
            /// <summary>
            /// Notify on the falling edge during a change
            /// </summary>
            Falling,
            /// <summary>
            /// Notify on any edge during a change
            /// </summary>
            Any
        }
        /// <summary>
        /// Input pin configuration types
        /// </summary>
        public enum PullMode {
            Up,
            Down,
            None
        }

        /// <summary>
        /// Data producer representing analog data from a gpio pin
        /// </summary>
        public interface IAnalogDataProducer : IDataProducer {
            /// <summary>
            /// Issues a read command to the analog pin
            /// </summary>
            /// <param name="pullup">Pin to be pulled up before the read, unused by default</param>
            /// <param name="pulldown">Pin to be pulled down before the read, unused by default</param>
            /// <param name="delay">How long to wait before reading from the pin, no delay by default</param>
            /// <param name="virtualPin">Directs read data to the specific virtual pin</param>
            void Read(byte pullup = 0xff, byte pulldown = 0xff, ushort delay = 0, byte virtualPin = 0xff);
        }
        /// <summary>
        /// Represents a physical GPIO pin
        /// </summary>
        public interface IPin {
            /// <summary>
            /// Data producer representing analog ADC data.  Not all pins support analog inputs, property is null is unsupported
            /// </summary>
            IAnalogDataProducer Adc { get; }
            /// <summary>
            /// Data producer representing absolute reference data.  Not all pins support analog inputs, property is null is unsupported
            /// </summary>
            IAnalogDataProducer AbsoluteReference { get; }
            /// <summary>
            /// Data producer representing digital data
            /// </summary>
            IForcedDataProducer Digital { get; }
            /// <summary>
            /// Data producer representing pin monitoring data
            /// </summary>
            IAsyncDataProducer Monitor { get; }

            /// <summary>
            /// Set the pin change type to look for
            /// </summary>
            /// <param name="type">New pin change type</param>
            void SetChangeType(PinChangeType type);
            /// <summary>
            /// Set the pin pull mode
            /// </summary>
            /// <param name="mode">New pull mode</param>
            void SetPullMode(PullMode mode);
            /// <summary>
            /// Clear the pin's output voltage i.e. logical low
            /// </summary>
            void ClearOutput();
            /// <summary>
            /// Set the pin's output voltage i.e. logical high
            /// </summary>
            void SetOutput();
        }
        /// <summary>
        /// Represents a virtual pin used by the <see cref="IAnalogDataProducer.Read(byte, byte, ushort, byte)"/> function
        /// </summary>
        public interface IVirtualPin {
            /// <summary>
            /// Data producer representing analog ADC data
            /// </summary>
            IDataProducer Adc { get; }
            /// <summary>
            /// Data producer representing absolute reference data
            /// </summary>
            IDataProducer AbsoluteReference { get; }
        }
    }
    
    /// <summary>
    /// General purpose I/O pins for connecting external sensors
    /// </summary>
    public interface IGpio : IModule {
        /// <summary>
        /// All physical GPIO pins on the board
        /// </summary>
        List<IPin> Pins { get; }
        /// <summary>
        /// Creates a virtual pin
        /// </summary>
        /// <param name="pin">Pin number to associate the virtual pin with, between [Pins.Length, 254]</param>
        /// <returns>Object representing the virtual pin</returns>
        IVirtualPin CreateVirtualPin(byte pin);
    }
}
