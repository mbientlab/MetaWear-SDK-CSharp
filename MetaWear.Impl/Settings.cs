using MbientLab.MetaWear.Core;
using System;

using System.Text;
using MbientLab.MetaWear.Core.Settings;
using static MbientLab.MetaWear.Impl.Module;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Settings : ModuleImplBase, ISettings {
        private const float AD_INTERVAL_STEP = 0.625f, CONN_INTERVAL_STEP = 1.25f, SUPERVISOR_TIMEOUT_STEP = 10f;
        private const byte CONN_PARAMS_REVISION = 1, DISCONNECTED_EVENT_REVISION = 2, BATTERY_REVISION = 3, CHARGE_STATUS_REVISION = 5;
        private const byte DEVICE_NAME = 1, AD_INTERVAL = 2, TX_POWER = 3,
            START_ADVERTISING = 5,
            SCAN_RESPONSE = 7, PARTIAL_SCAN_RESPONSE = 8,
            CONNECTION_PARAMS = 9,
            DISCONNECT_EVENT = 0xa,
            BATTERY_STATE = 0xc,
            POWER_STATUS = 0x11,
            CHARGE_STATUS = 0x12;

        [DataContract]
        class BatteryStateData : DataBase {
            public BatteryStateData(IModuleBoardBridge bridge, DataTypeBase datatype, DateTime timestamp, byte[] bytes) :
                base(bridge, datatype, timestamp, bytes) {
            }

            public override Type[] Types => new Type[] { typeof(BatteryState) };

            public override T Value<T>() {
                var type = typeof(T);

                if (type == typeof(BatteryState)) {
                    return (T)Convert.ChangeType(new BatteryState(bytes[0], BitConverter.ToUInt16(bytes, 1) / 1000f), type);
                }
                return base.Value<T>();
            }
        }

        [DataContract]
        private class BatteryStateDataType : DataTypeBase {
            internal BatteryStateDataType() : base(SETTINGS, Util.setRead(BATTERY_STATE), new DataAttributes(new byte[] { 1, 2 }, 1, 0, false)) { }

            internal BatteryStateDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) : base (input, module, register, id, attributes) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BatteryStateDataType(input, module, register, id, attributes);
            }

            public override IData createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new BatteryStateData(bridge, this, timestamp, data);
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new IntegralDataType(SETTINGS, eventConfig[1], eventConfig[2], new DataAttributes(new byte[] {1}, 1, 0, false)),
                    new MilliUnitsFloatDataType(SETTINGS, eventConfig[1], eventConfig[2], new DataAttributes(new byte[] {2}, 1, 1, false)),
                };
            }
        }

        private Timer adConfigReadTimeout, connParamsReadTimeout;
        private string deviceName;
        private ushort interval;
        private byte timeout;
        private sbyte txPower;
        private byte[] scanResponse;
        private TaskCompletionSource<BleAdvertisementConfig> adConfigTask;
        private TaskCompletionSource<BleConnectionParameters> connParamsTask;

        private IForcedDataProducer batteryProducer;
        private ActiveDataProducer<byte> powerStatusProducer, chargeStatusProducer;

        [DataMember] private DataTypeBase disconnectDummyProducer;
        [DataMember] private BatteryStateDataType batteryState;
        [DataMember] private IntegralDataType powerStatus, chargeStatus;

        public IForcedDataProducer Battery {
            get {
                if (batteryProducer == null && batteryState != null) {
                    batteryProducer = new ForcedDataProducer(batteryState, bridge);
                }
                return batteryProducer;
            }
        }

        public IActiveDataProducer<byte> PowerStatus {
            get {
                if (powerStatusProducer == null && powerStatus != null) {
                    powerStatusProducer = new ActiveDataProducer<byte>(powerStatus, bridge);
                }
                return powerStatusProducer;
            }
        }

        public IActiveDataProducer<byte> ChargeStatus {
            get {
                if (chargeStatusProducer == null && chargeStatus != null) {
                    chargeStatusProducer = new ActiveDataProducer<byte>(chargeStatus, bridge);
                }
                return chargeStatusProducer;
            }
        }

        public Settings(IModuleBoardBridge bridge) : base(bridge) {
            var info = bridge.lookupModuleInfo(SETTINGS);

            if (info.revision >= DISCONNECTED_EVENT_REVISION) {
                disconnectDummyProducer = new IntegralDataType(SETTINGS, DISCONNECT_EVENT, new DataAttributes(new byte[] { }, 0, 0, false));
            }
            if (info.revision >= BATTERY_REVISION) {
                batteryState = new BatteryStateDataType();
            }
            if (info.revision >= CHARGE_STATUS_REVISION && (info.extra.Length > 0 && (info.extra[0] & 0x1) == 0x1)) {
                powerStatus = new IntegralDataType(SETTINGS, POWER_STATUS, new DataAttributes(new byte[] { 1 }, 1, 0, false));
            }
            if (info.revision >= CHARGE_STATUS_REVISION && (info.extra.Length > 0 && (info.extra[0] & 0x2) == 0x2)) {
                chargeStatus = new IntegralDataType(SETTINGS, CHARGE_STATUS, new DataAttributes(new byte[] { 1 }, 1, 0, false));
            }
        }

        protected override void init() {
            bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(DEVICE_NAME)), response => {
                byte[] respBody = new byte[response.Length - 2];
                Array.Copy(response, 2, respBody, 0, respBody.Length);

                deviceName = Encoding.ASCII.GetString(respBody);
                bridge.sendCommand(new byte[] { (byte) SETTINGS, Util.setRead(AD_INTERVAL) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(TX_POWER)), response => {
                txPower = (sbyte)response[2];
                bridge.sendCommand(new byte[] { (byte)SETTINGS, Util.setRead(SCAN_RESPONSE) });
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(SCAN_RESPONSE)), response => {
                adConfigReadTimeout.Dispose();

                scanResponse = new byte[response.Length - 2];
                Array.Copy(response, 2, scanResponse, 0, scanResponse.Length);

                adConfigTask.SetResult(new BleAdvertisementConfig(deviceName, interval, timeout, txPower, scanResponse));
            });

            if (bridge.lookupModuleInfo(SETTINGS).revision >= CONN_PARAMS_REVISION) {
                bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(AD_INTERVAL)), response => {
                    interval = (ushort)(BitConverter.ToUInt16(response, 2) * AD_INTERVAL_STEP);
                    timeout = response[4];

                    bridge.sendCommand(new byte[] { (byte) SETTINGS, Util.setRead(TX_POWER) });
                });
                bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(CONNECTION_PARAMS)), response => {
                    connParamsReadTimeout.Dispose();

                    connParamsTask.SetResult(new BleConnectionParameters(
                            BitConverter.ToUInt16(response, 2) * CONN_INTERVAL_STEP,
                            BitConverter.ToUInt16(response, 4) * CONN_INTERVAL_STEP,
                            BitConverter.ToUInt16(response, 6),
                            (ushort)(BitConverter.ToUInt16(response, 8) * SUPERVISOR_TIMEOUT_STEP)));
                });
            } else {
                bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(AD_INTERVAL)), response => {
                    interval = BitConverter.ToUInt16(response, 2);
                    timeout = response[4];

                    bridge.sendCommand(new byte[] { (byte)SETTINGS, Util.setRead(TX_POWER) });
                });
            }

            if (bridge.lookupModuleInfo(SETTINGS).revision >= CHARGE_STATUS_REVISION) {
                bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(POWER_STATUS)), response => powerStatusProducer.SetReadResult(response[2]));
                bridge.addRegisterResponseHandler(Tuple.Create((byte)SETTINGS, Util.setRead(CHARGE_STATUS)), response => chargeStatusProducer.SetReadResult(response[2]));
            }
        }

        public void EditBleConnParams(float minConnInterval = 7.5F, float maxConnInterval = 125, ushort slaveLatency = 0, ushort supervisorTimeout = 6000) {
            if (bridge.lookupModuleInfo(SETTINGS).revision >= CONN_PARAMS_REVISION) {
                byte[] parameters = new byte[8];
                Array.Copy(Util.ushortToBytesLe((ushort)(minConnInterval / CONN_INTERVAL_STEP)), 0, parameters, 0, 2);
                Array.Copy(Util.ushortToBytesLe((ushort)(maxConnInterval / CONN_INTERVAL_STEP)), 0, parameters, 2, 2);
                Array.Copy(Util.ushortToBytesLe(slaveLatency), 0, parameters, 4, 2);
                Array.Copy(Util.ushortToBytesLe((ushort)(supervisorTimeout / SUPERVISOR_TIMEOUT_STEP)), 0, parameters, 6, 2);

                bridge.sendCommand(SETTINGS, CONNECTION_PARAMS, parameters);
            }
        }

        public async Task<IObserver> OnDisconnectAsync(Action commands) {
            if (disconnectDummyProducer == null) {
                TaskCompletionSource<IObserver> taskSource = new TaskCompletionSource<IObserver>();
                taskSource.SetException(new InvalidOperationException("Responding to disconnect events on-board is not supported on this firmware"));
                return await taskSource.Task;
            }
            return await bridge.queueObserverAsync(commands, disconnectDummyProducer);
        }

        public Task<BleAdvertisementConfig> ReadBleAdConfigAsync() {
            adConfigTask = new TaskCompletionSource<BleAdvertisementConfig>();
            adConfigReadTimeout = new Timer(e => adConfigTask.SetException(new TimeoutException("Timed out reading advertising config ")), null, 4 * 250, Timeout.Infinite);
            bridge.sendCommand(new byte[] { (byte)SETTINGS, Util.setRead(DEVICE_NAME) });

            return adConfigTask.Task;
        }

        public Task<BleConnectionParameters> ReadBleConnParamsAsync() {
            if (bridge.lookupModuleInfo(SETTINGS).revision >= CONN_PARAMS_REVISION) {
                connParamsTask = new TaskCompletionSource<BleConnectionParameters>();
                connParamsReadTimeout = new Timer(e => adConfigTask.SetException(new TimeoutException("Timed out reading connection parameters")), null, 250, Timeout.Infinite);
                bridge.sendCommand(new byte[] { (byte)SETTINGS, Util.setRead(CONNECTION_PARAMS) });

                return connParamsTask.Task;
            } else {
                return Task.FromException< BleConnectionParameters>(new NotSupportedException("Using btle connection parameters is not supported on this firmware version"));
            }
        }

        public void EditBleAdConfig(string name = null, byte? timeout = null, ushort? interval = null, sbyte? txPower = null, byte[] scanResponse = null) {
            if (name != null) {
                bridge.sendCommand(SETTINGS, DEVICE_NAME, Encoding.ASCII.GetBytes(name));
            }

            if (timeout != null || interval != null) {
                if (interval == null) {
                    interval = 0;
                }
                if (bridge.lookupModuleInfo(SETTINGS).revision >= CONN_PARAMS_REVISION) {
                    interval = (ushort)(interval / AD_INTERVAL_STEP);
                }
                byte[] config = new byte[3];
                Array.Copy(Util.ushortToBytesLe(interval.Value), config, 2);
                config[2] = timeout == null ? (byte) 0 : timeout.Value;

                bridge.sendCommand(SETTINGS, AD_INTERVAL, config);
            }

            if (txPower != null) {
                bridge.sendCommand(new byte[] { (byte) SETTINGS, TX_POWER, (byte)txPower });
            }

            if (scanResponse != null) {
                if (scanResponse.Length >= MetaWearBoard.COMMAND_LENGTH) {
                    byte[] first = new byte[13], second = new byte[scanResponse.Length - 13];
                    Array.Copy(scanResponse, 0, first, 0, first.Length);
                    Array.Copy(scanResponse, first.Length, second, 0, second.Length);

                    bridge.sendCommand(SETTINGS, PARTIAL_SCAN_RESPONSE, first);
                    bridge.sendCommand(SETTINGS, SCAN_RESPONSE, second);
                } else {
                    bridge.sendCommand(SETTINGS, SCAN_RESPONSE, scanResponse);
                }
            }
        }

        public void StartBleAdvertising() {
            bridge.sendCommand(new byte[] { (byte)SETTINGS, START_ADVERTISING });
        }
    }
}
