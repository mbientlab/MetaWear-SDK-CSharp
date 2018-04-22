using MbientLab.MetaWear.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Core {
    namespace Settings {
        /// <summary>
        /// Wrapper class encapsulating the battery state data
        /// </summary>
        public class BatteryState {
            /// <summary>
            /// Percent charged, between [0, 100]
            /// </summary>
            public byte Charge { get; }
            /// <summary>
            /// Battery voltage level in Volts (V)
            /// </summary>
            public float Voltage { get; }

            public BatteryState(byte charge, float voltage) {
                Charge = charge;
                Voltage = voltage;
            }

            public override string ToString() {
                return string.Format("{{Charge: {0:d}%, Voltage: {1:F3}V{2}", Charge, Voltage, "}");
            }

            public override bool Equals(Object obj) {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;

                BatteryState battery = obj as BatteryState;

                return battery.Charge == Charge && battery.Voltage.Equals(Voltage);
            }

            public override int GetHashCode() {
                int result = Charge;
                result = 31 * result + Voltage.GetHashCode();
                return result;
            }
        }
        /// <summary>
        /// Bluetooth LE advertising configuration
        /// </summary>
        public class BleAdvertisementConfig {
            /// <summary>
            /// Name the device advertises as
            /// </summary>
            public String DeviceName { get; }
            /// <summary>
            /// Time between each advertise event, in milliseconds (ms)
            /// </summary>
            public ushort Interval { get; }
            /// <summary>
            /// How long the device should advertise for with 0 indicating no timeout, in seconds (s)
            /// </summary>
            public byte Timeout { get; }
            /// <summary>
            /// Scan response
            /// </summary>
            public byte[] ScanResponse { get; }

            public BleAdvertisementConfig(String deviceName, ushort interval, byte timeout, byte[] scanResponse) {
                DeviceName = deviceName;
                Interval = interval;
                Timeout = timeout;
                ScanResponse = scanResponse;
            }

            public override string ToString() {
                return string.Format("{{Device Name: {0}, Adv Interval: {1:d}, Adv Timeout: {2:d}, Scan Response: [0x{3}]{4}",
                    DeviceName, Interval, Timeout, BitConverter.ToString(ScanResponse).ToLower().Replace("-", ", 0x"), "}");
            }

            public override bool Equals(Object obj) {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;

                BleAdvertisementConfig config = obj as BleAdvertisementConfig;

                return config.DeviceName.Equals(DeviceName) && config.ScanResponse.SequenceEqual(ScanResponse) && 
                    config.Interval == Interval && config.Timeout == Timeout;
            }

            public override int GetHashCode() {
                int result = DeviceName.GetHashCode();
                result = 31 * result + Interval;
                result = 31 * result + Timeout;
                result = 31 * result + EqualityComparer<byte[]>.Default.GetHashCode(ScanResponse); ;
                return result;
            }
        }
        /// <summary>
        /// Wrapper class containing BluetoothLe connection parameters
        /// </summary>
        public class BleConnectionParameters {
            /// <summary>
            /// Minimum time the central device asks for data from the peripheral, in milliseconds (ms)
            /// </summary>
            public float MinConnectionInterval { get; }
            /// <summary>
            /// Maximum time the central device asks for data from the peripheral, in milliseconds (ms)
            /// </summary>
            public float MaxConnectionInterval { get; }
            /// <summary>
            /// How many times the peripheral can choose to discard data requests from the central device
            /// </summary>
            public ushort SlaveLatency { get; }
            /// <summary>
            /// Timeout from the last data exchange until the ble link is considered lost
            /// </summary>
            public ushort SupervisorTimeout { get; }

            public BleConnectionParameters(float minConnectionInterval, float maxConnectionInterval, ushort slaveLatency, ushort supervisorTimeout) {
                MinConnectionInterval = minConnectionInterval;
                MaxConnectionInterval = maxConnectionInterval;
                SlaveLatency = slaveLatency;
                SupervisorTimeout = supervisorTimeout;
            }

            public override bool Equals(Object o) {
                if (this == o) return true;
                if (o == null || GetType() != o.GetType()) return false;

                BleConnectionParameters that = (BleConnectionParameters)o;

                return MinConnectionInterval.Equals(that.MinConnectionInterval) && MaxConnectionInterval.Equals(that.MaxConnectionInterval) &&
                        SlaveLatency == that.SlaveLatency && SupervisorTimeout == that.SupervisorTimeout;

            }

            public override int GetHashCode() {
                int result = (MinConnectionInterval != +0.0f ? BitConverter.ToInt32(BitConverter.GetBytes(MinConnectionInterval), 0) : 0);
                result = 31 * result + (MaxConnectionInterval != +0.0f ? BitConverter.ToInt32(BitConverter.GetBytes(MaxConnectionInterval), 0) : 0);
                result = 31 * result + SlaveLatency;
                result = 31 * result + SupervisorTimeout;
                return result;
            }

            public override string ToString() {
                return string.Format("{{min conn interval: {0:F2}, max conn interval: {1:F2}, slave latency: {2}, supervisor timeout: {3}{4}",
                        MinConnectionInterval, MaxConnectionInterval, SlaveLatency, SupervisorTimeout, "}");
            }
        }
    }
    /// <summary>
    /// Configures Bluetooth settings and auxiliary hardware and firmware features
    /// </summary>
    public interface ISettings : IModule {
        /// <summary>
        /// Data producer representing the battery state
        /// <para>This property returns null if the current board or firmware does not report battery information</para>
        /// </summary>
        IForcedDataProducer Battery { get; }
        /// <summary>
        /// Data producer representing the power status.
        /// <para>This property returns null if the current board or firmware does not support power status notifications</para>
        /// </summary>
        IActiveDataProducer<byte> PowerStatus { get; }
        /// <summary>
        /// <para>This property returns null if the current board or firmware does not support charge status notifications</para>
        /// </summary>
        IActiveDataProducer<byte> ChargeStatus { get; }

        /// <summary>
        /// Read the current ble advertising configuration
        /// </summary>
        /// <returns>Object representing the ad config</returns>
        Task<BleAdvertisementConfig> ReadBleAdConfigAsync();
        /// <summary>
        /// Read the current ble connection parameters
        /// </summary>
        /// <returns>Object representing the ble connection parameters</returns>
        /// <exception cref="NotSupportedException">If the firmware revision does not support ble connection parameters</exception>
        Task<BleConnectionParameters> ReadBleConnParamsAsync();
        /// <summary>
        /// Reads the radio's current transmitting power
        /// </summary>
        /// <returns>Tx power</returns>
        Task<sbyte> ReadTxPowerAsync();

        /// <summary>
        /// Sets the radio's transmitting power
        /// </summary>
        /// <param name="power">One of: 4, 0, -4, -8, -12, -16, -20, or -30</param>
        void SetTxPower(sbyte power);
        /// <summary>
        /// Edit the ble connection parameters
        /// </summary>
        /// <param name="minConnInterval">Lower bound of the connection interval, min 7.5ms</param>
        /// <param name="maxConnInterval">Upper bound of the connection interval, max 4000ms</param>
        /// <param name="slaveLatency">Number of connection intervals to skip, between [0, 1000]</param>
        /// <param name="supervisorTimeout">Maximum amount of time between data exchanges until the connection is considered to be lost, between [10, 32000] ms</param>
        void EditBleConnParams(float minConnInterval = 7.5f, float maxConnInterval = 125f, ushort slaveLatency = 0, ushort supervisorTimeout = 6000);
        /// <summary>
        /// Edit the ble advertising configuration
        /// </summary>
        /// <param name="name">Advertising name, max of 8 ASCII characters</param>
        /// <param name="timeout">Time between advertise events, in milliseconds (ms)</param>
        /// <param name="interval">How long to advertise for, between [0, 180] seconds where 0 indicates no timeout</param>
        /// <param name="scanResponse">Custom scan response packet</param>
        void EditBleAdConfig(string name = null, byte? timeout = null, ushort? interval = null, byte[] scanResponse = null);
        /// <summary>
        /// Starts ble advertising
        /// </summary>
        void StartBleAdvertising();

        /// <summary>
        /// Programs a task to the board that will be executed when a disconnect occurs
        /// </summary>
        /// <param name="commands">MetaWear commands composing the task</param>
        /// <returns>Observer object representing the task</returns>
        Task<IObserver> OnDisconnectAsync(Action commands);
    }
}
