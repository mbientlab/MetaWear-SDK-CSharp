using MbientLab.MetaWear.Peripheral.SerialPassthrough;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Peripheral {
    namespace SerialPassthrough {
        /// <summary>
        /// Supported SPI frequencies
        /// </summary>
        public enum SpiFrequency {
            _125_KHz,
            _250_KHz,
            _500_KHz,
            _1_MHz,
            _2_MHz,
            _4_MHz,
            _8_MHz
        }
        /// <summary>
        /// Data producer representing data from the I2C bus
        /// </summary>
        public interface II2CDataProducer : IDataProducer {
            /// <summary>
            /// Read data via the I2C bus
            /// </summary>
            /// <param name="deviceAddr">Device to read from</param>
            /// <param name="registerAddr">Device's register to read</param>
            void Read(byte deviceAddr, byte registerAddr);
        }
        /// <summary>
        /// Data received from the SPI bus
        /// </summary>
        public interface ISPIDataProducer : IDataProducer {
            /// <summary>
            /// Read data via the SPI bus
            /// </summary>
            /// <param name="slaveSelectPin">Pin for slave select</param>
            /// <param name="clockPin">Pin for serial clock</param>
            /// <param name="mosiPin">Pin for master output, slave input</param>
            /// <param name="misoPin">Pin for master input, slave output</param>
            /// <param name="mode">SPI operating mode, see <a href="https://en.wikipedia.org/wiki/Serial_Peripheral_Interface_Bus#Mode_numbers">SPI Wiki Page</a></param>
            /// <param name="frequency">SPI operating frequency</param>
            /// <param name="data">Data to write to the device before the read, defaults to null</param>
            /// <param name="lsbFirst">True to have LSB sent first, defaults to true</param>
            /// <param name="useNativePins">True to use the nRF pin mappings rather than the GPIO pin mappings, defaults to true</param>
            void Read(byte slaveSelectPin, byte clockPin, byte mosiPin, byte misoPin, byte mode, SpiFrequency frequency,
                byte[] data = null, bool lsbFirst = true, bool useNativePins = true);
        }
    }
    /// <summary>
    /// Bridge for serial communication to connected sensors
    /// </summary>
    public interface ISerialPassthrough : IModule {
        /// <summary>
        /// Get an object representing the I2C data corresponding to the id.  If the id value cannot be matched
        /// with an existing object, the API will create a new object using the <code>length</code>
        /// parameter otherwise the existing object will be returned
        /// </summary>
        /// <param name="id">Value representing the i2c data, between [0, 254]</param>
        /// <param name="length">Expected length of the data</param>
        /// <returns>Object representing I2C data</returns>
        II2CDataProducer I2C(byte id, byte length);
        /// <summary>
        /// Write data to a connected device via the I2C bus.
        /// </summary>
        /// <param name="deviceAddr">Device to write to</param>
        /// <param name="registerAddr">Device's register to write to</param>
        /// <param name="data">Data to write, up to 10 bytes</param>
        void WriteI2C(byte deviceAddr, byte registerAddr, byte[] data);
        /// <summary>
        /// Read data from a sensor via the I2C bus.  Unlike <see cref="II2CDataProducer.Read(byte, byte)"/>, this function provides
        /// a direct way to access I2C data as opposed to creating a data route.
        /// </summary>
        /// <param name="deviceAddr">Address of the slave device</param>
        /// <param name="registerAddr">Register on the slave device to access</param>
        /// <param name="length">How many bytes to read</param>
        /// <returns>Data read via the I2C bus</returns>
        Task<byte[]> ReadI2CAsync(byte deviceAddr, byte registerAddr, byte length);

        /// <summary>
        /// Get an object representing the SPI data corresponding to the id.  If the id value cannot be matched
        /// with an existing object, the API will create a new object using the <code>length</code>
        ///parameter otherwise the existing object will be returned
        /// </summary>
        /// <param name="id">Value representing the i2c data, between [0, 14]</param>
        /// <param name="length">Expected length of the data</param>
        /// <returns>Object representing SPI data</returns>
        ISPIDataProducer SPI(byte id, byte length);
        /// <summary>
        /// Write data to a connected device via the SPI bus
        /// </summary>
        /// <param name="slaveSelectPin">Pin for slave select</param>
        /// <param name="clockPin">Pin for serial clock</param>
        /// <param name="mosiPin">Pin for master output, slave input</param>
        /// <param name="misoPin">Pin for master input, slave output</param>
        /// <param name="mode">SPI operating mode, see <a href="https://en.wikipedia.org/wiki/Serial_Peripheral_Interface_Bus#Mode_numbers">SPI Wiki Page</a></param>
        /// <param name="frequency">SPI operating frequency</param>
        /// <param name="data">Data to write to the device</param>
        /// <param name="lsbFirst">True to have LSB sent first, defaults to true</param>
        /// <param name="useNativePins">True to use the nRF pin mappings rather than the GPIO pin mappings, defaults to true</param>
        void WriteSPI(byte slaveSelectPin, byte clockPin, byte mosiPin, byte misoPin, byte mode, SpiFrequency frequency,
            byte[] data, bool lsbFirst = true, bool useNativePins = true);
        /// <summary>
        /// Read data from a sensor via the SPI bus.  Unlike <see cref="ISPIDataProducer.Read(byte, byte, byte, byte, byte, SpiFrequency, bool, bool, byte[])"/>, 
        /// this function provides a direct way to access SPI data as opposed to creating a data route.
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        /// <param name="slaveSelectPin">Pin for slave select</param>
        /// <param name="clockPin">Pin for serial clock</param>
        /// <param name="mosiPin">Pin for master output, slave input</param>
        /// <param name="misoPin">Pin for master input, slave output</param>
        /// <param name="mode">SPI operating mode, see <a href="https://en.wikipedia.org/wiki/Serial_Peripheral_Interface_Bus#Mode_numbers">SPI Wiki Page</a></param>
        /// <param name="frequency">SPI operating frequency</param>
        /// <param name="data">Data to write to the device before the read, defaults to null</param>
        /// <param name="lsbFirst">True to have LSB sent first, defaults to true</param>
        /// <param name="useNativePins">True to use the nRF pin mappings rather than the GPIO pin mappings, defaults to true</param>
        /// <returns>Data received from the read command</returns>
        Task<byte[]> ReadSPIAsync(byte length, byte slaveSelectPin, byte clockPin, byte mosiPin, byte misoPin, byte mode, SpiFrequency frequency,
            byte[] data = null, bool lsbFirst = true, bool useNativePins = true);
    }
}
