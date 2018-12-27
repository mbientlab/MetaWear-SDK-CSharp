using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using static MbientLab.MetaWear.Impl.RouteComponent;
using MbientLab.MetaWear.Core.DataProcessor;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    abstract class EditorImplBase : SerializableType, IEditor {
        [DataMember] internal byte[] config;
        [DataMember] internal readonly DataTypeBase source;
        internal DataProcessorConfig configObj;

        internal EditorImplBase(DataProcessorConfig configObj, DataTypeBase source, IModuleBoardBridge bridge) : base(bridge) {
            config = configObj.Build();
            this.source = source;
            this.configObj = configObj;
        }
    }

    [DataContract]
    class NullEditor : EditorImplBase {
        internal NullEditor(DataProcessorConfig configObj, DataTypeBase source, IModuleBoardBridge bridge) : base(configObj, source, bridge) { }
    }

    [KnownType(typeof(AccumulatorEditorInner))]
    [KnownType(typeof(AverageEditorInner))]
    [KnownType(typeof(CounterEditorInner))]
    [KnownType(typeof(SingleValueComparatorEditor))]
    [KnownType(typeof(MultiValueComparatorEditor))]
    [KnownType(typeof(MapEditorInner))]
    [KnownType(typeof(PassthroughEditorInner))]
    [KnownType(typeof(PulseEditorInner))]
    [KnownType(typeof(ThresholdEditorInner))]
    [KnownType(typeof(DifferentialEditorInner))]
    [KnownType(typeof(TimeEditorInner))]
    [KnownType(typeof(PackerEditorInner))]
    [KnownType(typeof(NullEditor))]
    [KnownType(typeof(DataTypeBase))]
    [DataContract]
    class DataProcessor : ModuleImplBase, IDataProcessor {
        internal static string createIdentifier(DataTypeBase dataType, DataProcessor dataprocessor, Version firmware, byte revision) {
            byte register = Util.clearRead(dataType.eventConfig[1]);
            switch (register) {
                case NOTIFY:
                case STATE:
                    var processor = dataprocessor.lookupProcessor(dataType.eventConfig[2]);
                    DataProcessorConfig config = DataProcessorConfig.from(firmware, revision, processor.Item2.config);

                    return config.CreateIdentifier(register == STATE, dataType.eventConfig[2]);
                default:
                    return null;
            }
        }

        internal class ProcessorEntry {
            internal byte id, offset, length;
            internal byte[] source, config;
        }

        internal const byte TYPE_ACCOUNTER = 0x11, TYPE_PACKER = 0x10;
        internal const byte TIME_PASSTHROUGH_REVISION = 1, ENHANCED_STREAMING_REVISION = 2, HPF_REVISION = 2, EXPANDED_DELAY = 2, FUSE_REVISION = 3;
        internal const byte ADD = 2,
                NOTIFY = 3,
                STATE = 4,
                PARAMETER = 5,
                REMOVE = 6,
                NOTIFY_ENABLE = 7,
                REMOVE_ALL = 8;

        [DataMember] internal Dictionary<byte, Tuple<DataTypeBase, EditorImplBase>> activeProcessors= new Dictionary<byte, Tuple<DataTypeBase, EditorImplBase>>();
        [DataMember] internal Dictionary<string, byte> nameToId = new Dictionary<string, byte>();
 
        private TimedTask<byte> createProcessorTask;
        private TimedTask<byte[]> pullProcessorConfigTask;
        private Dictionary<string, IForcedDataProducer> stateDataProducers;

        public DataProcessor(IModuleBoardBridge bridge) : base(bridge) {
        }

        public override void tearDown() {
            bridge.sendCommand(new byte[] { (byte)DATA_PROCESSOR, REMOVE_ALL });
            foreach(var it in nameToId.Keys) {
                bridge.removeProducerName(it);
            }
            activeProcessors.Clear();
            nameToId.Clear();
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            base.restoreTransientVars(bridge);

            foreach (Tuple<DataTypeBase, EditorImplBase> it in activeProcessors.Values) {
                it.Item2.restoreTransientVars(bridge);
            }
        }

        protected override void init() {
            createProcessorTask = new TimedTask<byte>();
            pullProcessorConfigTask = new TimedTask<byte[]>();

            stateDataProducers = new Dictionary<string, IForcedDataProducer>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte) DATA_PROCESSOR, ADD), response => createProcessorTask.SetResult(response[2]));
            bridge.addRegisterResponseHandler(Tuple.Create((byte)DATA_PROCESSOR, Util.setRead(ADD)), response => pullProcessorConfigTask.SetResult(response));
        }

        public IForcedDataProducer State(string name) {
            if (stateDataProducers.TryGetValue(name, out var producer)) {
                return producer;
            }
            if (nameToId.TryGetValue(name, out var id) && activeProcessors.TryGetValue(id, out var value)) {
                producer = new ForcedDataProducer(value.Item1, bridge);
                stateDataProducers.Add(name, producer);
                return producer;
            }
            return null;
        }

        public T Edit<T>(string name) where T : class, IEditor {
            return nameToId.TryGetValue(name, out var id) && activeProcessors.TryGetValue(id, out var value) ?
                value.Item2 as T : null;
        }

        internal void remove(byte id, bool sync) {
            activeProcessors.Remove(id);
            if (sync) {
                bridge.sendCommand(new byte[] { (byte)DATA_PROCESSOR, REMOVE, id });
            }
        }

        internal async Task<Queue<byte>> queueDataProcessors(LinkedList<Tuple<DataTypeBase, EditorImplBase>> pendingProcessors) {
            var successfulProcessors = new Queue<byte>();
            try {
                while (pendingProcessors.Count != 0) {
                    Tuple<DataTypeBase, EditorImplBase> current = pendingProcessors.First.Value;
                    DataTypeBase input = current.Item2.source.input;

                    if (current.Item2.configObj is DataProcessorConfig.FuserConfig) {
                        (current.Item2.configObj as DataProcessorConfig.FuserConfig).SyncFilterIds(this);
                    }

                    byte[] filterConfig = new byte[input.eventConfig.Length + 1 + current.Item2.config.Length];
                    filterConfig[input.eventConfig.Length] = (byte)(((input.attributes.length() - 1) << 5) | input.attributes.offset);
                    Array.Copy(input.eventConfig, 0, filterConfig, 0, input.eventConfig.Length);
                    Array.Copy(current.Item2.config, 0, filterConfig, input.eventConfig.Length + 1, current.Item2.config.Length);

                    var id = await createProcessorTask.Execute("Did not receive data processor id within {0}ms", bridge.TimeForResponse,
                        () => bridge.sendCommand(DATA_PROCESSOR, ADD, filterConfig));

                    pendingProcessors.RemoveFirst();

                    current.Item2.source.eventConfig[2] = id;
                    if (current.Item2.source.components != null) {
                        foreach(var c in current.Item2.source.components) {
                            c.eventConfig[2] = id;
                        }
                    }
                    if (current.Item1 != null) {
                        current.Item1.eventConfig[2] = id;
                    }
                    activeProcessors[id] = current;
                    successfulProcessors.Enqueue(id);
                }
            } catch (TimeoutException e) {
                foreach (byte it in successfulProcessors) {
                    removeProcessor(true, it);
                }
                throw e;
            }
            return successfulProcessors;
        }

        void removeProcessor(bool sync, byte id) {
            if (sync && activeProcessors.TryGetValue(id, out Tuple<DataTypeBase, EditorImplBase> value)) {
                bridge.sendCommand(new byte[] { (byte) DATA_PROCESSOR, REMOVE, value.Item2.source.eventConfig[2] });
            }

            activeProcessors.Remove(id);
        }

        internal Tuple<DataTypeBase, EditorImplBase> lookupProcessor(byte id) {
            return activeProcessors.TryGetValue(id, out var result) ? result : null;
        }

        internal async Task<Stack<ProcessorEntry>> pullChainAsync(byte id) {
            var entries = new Stack<ProcessorEntry>();
            var terminate = false;
            var readId = id;

            while (!terminate) {
                if (activeProcessors.TryGetValue(readId, out var processor)) {
                    var entry = new ProcessorEntry {
                        id = readId,
                        config = processor.Item2.config
                    };
                    entries.Push(entry);

                    if (processor.Item2.source.eventConfig[0] == (byte)DATA_PROCESSOR) {
                        readId = processor.Item2.source.eventConfig[2];
                    } else {
                        terminate = true;
                    }
                } else {
                    var config = await pullProcessorConfigTask.Execute("Did not receive data processor config within {0}ms", bridge.TimeForResponse,
                        () => bridge.sendCommand(new byte[] { (byte)DATA_PROCESSOR, Util.setRead(ADD), readId }));

                    var entry = new ProcessorEntry {
                        id = readId,
                        offset = (byte)(config[5] & 0x1f),
                        length = (byte)(((config[5] >> 5) & 0x7) + 1),
                        source = new byte[3],
                        config = new byte[config.Length - 6]
                    };

                    Array.Copy(config, 2, entry.source, 0, entry.source.Length);
                    Array.Copy(config, 6, entry.config, 0, entry.config.Length);

                    entries.Push(entry);
                    if (config[2] == (byte)DATA_PROCESSOR) {
                        readId = config[4];
                    } else {
                        terminate = true;
                    }
                }
            }

            return entries;
        }
    }
}
