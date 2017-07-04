using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class LoggedDataConsumer : DeviceDataConsumer {
        [DataMember] private readonly Dictionary<byte, Queue<byte[]>> logEntries= new Dictionary<byte, Queue<byte[]>>();
        [DataMember] private readonly List<byte> orderedIds = new List<byte>();

        internal LoggedDataConsumer(DataTypeBase source) : base(source) {
        }

        internal void addId(byte id) {
            orderedIds.Add(id);
            logEntries.Add(id, new Queue<byte[]>());
        }

        public void remove(IModuleBoardBridge bridge, bool sync) {
            Logging logging = bridge.GetModule<ILogging>() as Logging;
            orderedIds.ForEach(id => logging.Remove(id, sync));
        }

        internal void register(Dictionary<byte, LoggedDataConsumer> loggers) {
            orderedIds.ForEach(id => loggers.Add(id, this));
        }

        internal void handleLogMessage(IModuleBoardBridge bridge, byte logId, DateTime timestamp, byte[] data, Action<LogDownloadError, byte, DateTime, byte[]> errorHandler) {
            if (subscriber == null) {
                errorHandler?.Invoke(LogDownloadError.UNHANDLED_LOG_DATA, logId, timestamp, data);
                return;
            }

            if (logEntries.TryGetValue(logId, out var logEntry)) {
                logEntry.Enqueue(data);
            } else {
                errorHandler?.Invoke(LogDownloadError.UNKNOWN_LOG_ENTRY, logId, timestamp, data);
            }

            if (logEntries.Values.Aggregate(true, (acc, e) => acc && e.Count != 0)) {
                byte[] merged = new byte[source.attributes.length()];
                int offset = 0;

                orderedIds.Aggregate(new List<byte[]>(), (acc, id) => {
                    logEntries.TryGetValue(id, out var cached);
                    acc.Add(cached.Dequeue());
                    return acc;
                }).ForEach(entry => {
                    Array.Copy(entry, 0, merged, offset, Math.Min(entry.Length, source.attributes.length() - offset));
                    offset += entry.Length;
                });

                call(source.createData(true, bridge, merged, timestamp));
            }
        }

        public override void enableStream(IModuleBoardBridge bridge) { }

        public override void disableStream(IModuleBoardBridge bridge) { }

        public override void addDataHandler(IModuleBoardBridge bridge) { }
    }

    [DataContract]
    class Logging : ModuleImplBase, ILogging {
        private const double TICK_TIME_STEP = (48.0 / 32768.0) * 1000.0;
        private const byte LOG_ENTRY_SIZE = 4, REVISION_EXTENDED_LOGGING = 2;
        internal const byte ENABLE = 1,
            TRIGGER = 2,
            REMOVE = 3,
            TIME = 4,
            LENGTH = 5,
            READOUT = 6, READOUT_NOTIFY = 7, READOUT_PROGRESS = 8,
            REMOVE_ENTRIES = 9, REMOVE_ALL = 0xa,
            CIRCULAR_BUFFER = 0xb,
            READOUT_PAGE_COMPLETED = 0xd, READOUT_PAGE_CONFIRM = 0xe;

        [DataMember] private Tuple<byte, uint, DateTime> latestReference;
        [DataMember] private Dictionary<byte, Tuple<byte, uint, DateTime>> logReferenceTicks= new Dictionary<byte, Tuple<byte, uint, DateTime>>();
        [DataMember] private Dictionary<byte, LoggedDataConsumer> dataLoggers= new Dictionary<byte, LoggedDataConsumer>();
        [DataMember] private Dictionary<byte, uint> lastTimestamp= new Dictionary<byte, uint>();

        private TaskCompletionSource<bool> queryTimeTask;
        
        public Logging(IModuleBoardBridge bridge) : base(bridge) {
        }

        public override void tearDown() {
            bridge.sendCommand(new byte[] { (byte)LOGGING, REMOVE_ALL });
            dataLoggers.Clear();
        }

        public override void disconnected() {
            if (downloadTask != null) {
                downloadTask.SetCanceled();
                downloadTask = null;
            }
        }

        private void processLogData(byte[] logEntry, int offset) {
            byte logId = (byte)(logEntry[0 + offset] & 0x1f), resetUid = (byte)((logEntry[0 + offset] & ~0x1f) >> 5);
            if (!logReferenceTicks.TryGetValue(resetUid, out var reference)) {
                reference = latestReference;
            }

            uint tick = BitConverter.ToUInt32(logEntry, 1 + offset);
            DateTime timestamp = reference.Item3.AddMilliseconds((uint) ((tick - reference.Item2) * TICK_TIME_STEP));

            if (!lastTimestamp.TryGetValue(logId, out var cachedTick) || cachedTick < tick) { 
                lastTimestamp[logId] = tick;

                byte[] logData = new byte[LOG_ENTRY_SIZE];
                Array.Copy(logEntry, 5 + offset, logData, 0, LOG_ENTRY_SIZE);

                if (dataLoggers.TryGetValue(logId, out var logger)) {
                    logger.handleLogMessage(bridge, logId, timestamp, logData, errorHandler);
                } else {
                    errorHandler?.Invoke(LogDownloadError.UNKNOWN_LOG_ENTRY, logId, timestamp, logData);
                }
            }
        }

        protected override void init() {
            bridge.addRegisterResponseHandler(Tuple.Create((byte)LOGGING, TRIGGER), response => {
                nReqLogIds--;
                nextLogger.addId(response[2]);

                if (nReqLogIds == 0) {
                    timeoutFuture.Dispose();
                    nextLogger.register(dataLoggers);
                    successfulLoggers.Enqueue(nextLogger);
                    createLogger();
                }
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)LOGGING, READOUT_NOTIFY), response => {
                processLogData(response, 2);

                if (response.Length == 20) {
                    processLogData(response, 11);
                }
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)LOGGING, READOUT_PROGRESS), response => {
                uint nEntriesLeft = BitConverter.ToUInt32(response, 2);

                if (nEntriesLeft == 0) {
                    lastTimestamp.Clear();
                    downloadTask.SetResult(true);
                    downloadTask = null;
                } else {
                    updateHandler?.Invoke(nEntriesLeft, nLogEntries);
                }
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)LOGGING, Util.setRead(TIME)), response => {
                uint tick = BitConverter.ToUInt32(response, 2);
                byte resetUid = (response.Length > 6) ? response[6] : (byte) 0xff;

                latestReference = Tuple.Create(resetUid, tick, DateTime.Now);
                if (resetUid != 0xff) {
                    logReferenceTicks[resetUid] = latestReference;
                }

                queryTimeTask.SetResult(true);
            });
            bridge.addRegisterResponseHandler(Tuple.Create((byte)LOGGING, Util.setRead(LENGTH)), response => {
                int payloadSize = response.Length - 2;
                nLogEntries = BitConverter.ToUInt32(response, 2);

                if (nLogEntries == 0) {
                    downloadTask.SetResult(true);
                    downloadTask = null;
                } else {
                    updateHandler?.Invoke(nLogEntries, nLogEntries);

                    uint nEntriesNotify = nUpdates == 0 ? 0 : (uint) (nLogEntries * (1.0 / nUpdates));
                    // In little endian, [A, B, 0, 0] is equal to [A, B]
                    byte[] command = new byte[payloadSize + sizeof(uint)];

                    Array.Copy(response, 2, command, 0, payloadSize);
                    Array.Copy(BitConverter.GetBytes(nEntriesNotify), 0, command, payloadSize, sizeof(uint));
                    
                    bridge.sendCommand(LOGGING, READOUT, command);
                }
            });

            if (bridge.lookupModuleInfo(LOGGING).revision >= REVISION_EXTENDED_LOGGING) {
                bridge.addRegisterResponseHandler(Tuple.Create((byte)LOGGING, READOUT_PAGE_COMPLETED), response => bridge.sendCommand(new byte[] { (byte)LOGGING, READOUT_PAGE_CONFIRM }));
            }
        }

        public void ClearEntries() {
            bridge.sendCommand(new byte[] { (byte) LOGGING, REMOVE_ENTRIES, 0xff, 0xff, 0xff, 0xff });
        }

        private uint nLogEntries, nUpdates;
        private TaskCompletionSource<bool> downloadTask;
        private Action<uint, uint> updateHandler;
        private Action<LogDownloadError, byte, DateTime, byte[]> errorHandler;

        public Task DownloadAsync(uint nUpdates, Action<uint, uint> updateHandler, Action<LogDownloadError, byte, DateTime, byte[]> errorHandler) {
            if (downloadTask != null) {
                return downloadTask.Task;
            }

            this.nUpdates = nUpdates;
            this.updateHandler = updateHandler;
            this.errorHandler = errorHandler;

            if (bridge.lookupModuleInfo(LOGGING).revision >= REVISION_EXTENDED_LOGGING) {
                bridge.sendCommand(new byte[] { (byte) LOGGING, READOUT_PAGE_COMPLETED, 1 });
            }
            bridge.sendCommand(new byte[] { (byte)LOGGING, READOUT_NOTIFY, 1 });
            bridge.sendCommand(new byte[] { (byte)LOGGING, READOUT_PROGRESS, 1 });
            bridge.sendCommand(new byte[] { (byte)LOGGING, Util.setRead(LENGTH) });

            downloadTask = new TaskCompletionSource<bool>();
            return downloadTask.Task;
        }

        public Task DownloadAsync(uint nUpdates, Action<uint, uint> updateHandler) {
            return DownloadAsync(nUpdates, updateHandler, null);
        }

        public Task DownloadAsync(Action<LogDownloadError, byte, DateTime, byte[]> errorHandler) {
            return DownloadAsync(0, null, errorHandler);
        }

        public Task DownloadAsync() {
            return DownloadAsync(0, null, null);
        }

        public void Start(bool overwrite = false) {
            bridge.sendCommand(new byte[] { (byte)LOGGING, CIRCULAR_BUFFER, (byte)(overwrite ? 1 : 0) });
            bridge.sendCommand(new byte[] { (byte)LOGGING, ENABLE, 1 });
        }

        public void Stop() {
            bridge.sendCommand(new byte[] { (byte)LOGGING, ENABLE, 0 });
        }

        internal Task QueryTimeAsync() {
            queryTimeTask = new TaskCompletionSource<bool>();
            bridge.sendCommand(new byte[] { (byte)LOGGING, Util.setRead(TIME) });
            return queryTimeTask.Task;
        }

        private byte nReqLogIds;
        private Timer timeoutFuture;
        private Queue<DataTypeBase> pendingProducers;
        private Queue<LoggedDataConsumer> successfulLoggers;
        private LoggedDataConsumer nextLogger;
        private TaskCompletionSource<Queue<LoggedDataConsumer>> createLoggerTask;

        internal void Remove(byte id, bool sync) {
            dataLoggers.Remove(id);
            if (sync) {
                bridge.sendCommand(new byte[] { (byte)LOGGING, REMOVE, id });
            }
        }
        internal Task<Queue<LoggedDataConsumer>> CreateLoggersAsync(Queue<DataTypeBase> producers) {
            pendingProducers = producers;
            createLoggerTask = new TaskCompletionSource<Queue<LoggedDataConsumer>>();
            successfulLoggers = new Queue<LoggedDataConsumer>();
            createLogger();
            return createLoggerTask.Task;
        }

        private void createLogger() {
            if (pendingProducers.Count != 0) {
                nextLogger = new LoggedDataConsumer(pendingProducers.Dequeue());
                byte[] eventConfig = nextLogger.source.eventConfig;

                nReqLogIds = (byte)((nextLogger.source.attributes.length() - 1) / LOG_ENTRY_SIZE + 1);
                int remainder = nextLogger.source.attributes.length();

                for (byte i = 0; i < nReqLogIds; i++, remainder -= LOG_ENTRY_SIZE) {
                    int entrySize = Math.Min(remainder, LOG_ENTRY_SIZE), entryOffset = LOG_ENTRY_SIZE * i + nextLogger.source.attributes.offset;

                    byte[] command = new byte[6];
                    command[0] = (byte) LOGGING;
                    command[1] = TRIGGER;
                    command[2 + eventConfig.Length] = (byte)(((entrySize - 1) << 5) | entryOffset);
                    Array.Copy(eventConfig, 0, command, 2, eventConfig.Length);

                    bridge.sendCommand(command);
                }
                timeoutFuture = new Timer(e => {
                    while (successfulLoggers.Count != 0) {
                        successfulLoggers.Dequeue().remove(bridge, true);
                    }
                    nextLogger.remove(bridge, true);
                    pendingProducers = null;
                    createLoggerTask.SetException(new TimeoutException("Creating logger timed out"));
                }, null, nReqLogIds * 250, Timeout.Infinite);
            } else {
                createLoggerTask.SetResult(successfulLoggers);
            }
        }
    }
}
