using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;
using static MbientLab.MetaWear.Impl.RouteComponent;
using MbientLab.MetaWear.Core.DataProcessor;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    abstract class EditorImplBase : SerializableType, IEditor {
        [DataMember] internal byte[] config;
        [DataMember] internal readonly DataTypeBase source;

        internal EditorImplBase(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(bridge) {
            this.config = config;
            this.source = source;
        }
    }

    [DataContract]
    class NullEditor : EditorImplBase {
        internal NullEditor(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }
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
    [KnownType(typeof(DataTypeBase))]
    [DataContract]
    class DataProcessor : ModuleImplBase, IDataProcessor {
        internal const byte TIME_PASSTHROUGH_REVISION = 1;
        internal const byte ADD = 2,
                NOTIFY = 3,
                STATE = 4,
                PARAMETER = 5,
                REMOVE = 6,
                NOTIFY_ENABLE = 7,
                REMOVE_ALL = 8;

        [DataMember] private Dictionary<byte, Tuple<DataTypeBase, EditorImplBase>> activeProcessors= new Dictionary<byte, Tuple<DataTypeBase, EditorImplBase>>();
        [DataMember] internal Dictionary<string, byte> nameToId = new Dictionary<string, byte>();
 
        private Timer createTimeout;
        private LinkedList<Tuple<DataTypeBase, EditorImplBase>> pendingProcessors;
        private Queue<byte> successfulProcessors;
        private TaskCompletionSource<Queue<byte>> createProcessorsTask;

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
            bridge.addRegisterResponseHandler(Tuple.Create((byte) DATA_PROCESSOR, ADD), response => {
                createTimeout.Dispose();

                Tuple<DataTypeBase, EditorImplBase> current = pendingProcessors.First.Value;
                pendingProcessors.RemoveFirst();

                current.Item2.source.eventConfig[2] = response[2];
                if (current.Item1 != null) {
                    current.Item1.eventConfig[2] = response[2];
                }
                activeProcessors[response[2]] = current;
                successfulProcessors.Enqueue(response[2]);

                createProcessor();
            });
        }

        public IForcedDataProducer State(string name) {
            throw new NotImplementedException();
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

        internal Task<Queue<byte>> queueDataProcessors(LinkedList<Tuple<DataTypeBase, EditorImplBase>> pendingProcessors) {
            successfulProcessors = new Queue<byte>();
            this.pendingProcessors = pendingProcessors;
            createProcessorsTask = new TaskCompletionSource<Queue<byte>>();
            createProcessor();
            return createProcessorsTask.Task;
        }

        private void createProcessor() {
            if (pendingProcessors.Count != 0) {
                Tuple<DataTypeBase, EditorImplBase> current = pendingProcessors.First.Value;
                DataTypeBase input = current.Item2.source.input;

                byte[] filterConfig = new byte[input.eventConfig.Length + 1 + current.Item2.config.Length];
                filterConfig[input.eventConfig.Length] = (byte)(((input.attributes.length() - 1) << 5) | input.attributes.offset);
                Array.Copy(input.eventConfig, 0, filterConfig, 0, input.eventConfig.Length);
                Array.Copy(current.Item2.config, 0, filterConfig, input.eventConfig.Length + 1, current.Item2.config.Length);

                bridge.sendCommand(DATA_PROCESSOR, ADD, filterConfig);

                createTimeout = new Timer(e => {
                    pendingProcessors = null;
                    foreach (byte it in successfulProcessors) {
                        removeProcessor(true, it);
                    }
                    createProcessorsTask.SetException(new TimeoutException("Creating data processor timed out"));
                }, null, 250, Timeout.Infinite);
            } else {
                createProcessorsTask.SetResult(successfulProcessors);
            }
        }

        void removeProcessor(bool sync, byte id) {
            if (sync && activeProcessors.TryGetValue(id, out Tuple<DataTypeBase, EditorImplBase> value)) {
                bridge.sendCommand(new byte[] { (byte) DATA_PROCESSOR, REMOVE, value.Item2.source.eventConfig[2] });
            }

            activeProcessors.Remove(id);
        }
    }
}
