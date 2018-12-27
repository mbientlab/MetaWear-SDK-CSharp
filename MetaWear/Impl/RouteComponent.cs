using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Core.DataProcessor;
using static MbientLab.MetaWear.Impl.DataProcessorConfig.MathConfig;
using static MbientLab.MetaWear.Impl.DataProcessorConfig;

namespace MbientLab.MetaWear.Impl {
    enum BranchType {
        Multicast,
        Split
    }

    class RouteMulticast : IRouteMulticast {
        private RouteComponent caller;

        internal RouteMulticast(RouteComponent caller) {
            this.caller = caller;
        }

        public IRouteComponent To() {
            return caller.state.stashedComponents.Peek().Item1;
        }
    }

    class RouteSplit : IRouteSplit {
        private RouteComponent caller;

        internal RouteSplit(RouteComponent caller) {
            this.caller = caller;
        }

        public IRouteComponent Index(int i) {
            try {
                return new RouteComponent(caller.state.splits.Peek().Item2[i], caller);
            } catch (IndexOutOfRangeException e) {
                throw new IllegalRouteOperationException("Index on split data out of bounds", e);
            }
        }
    }

    class RouteBranchEnd : IRouteBranchEnd {
        private RouteComponent caller;

        internal RouteBranchEnd(RouteComponent caller) {
            this.caller = caller;
        }

        public IRouteComponent Index(int i) {
            return caller.Index(i);
        }

        public IRouteBranchEnd Name(string name) {
            caller.Name(name);
            return this;
        }

        public IRouteComponent To() {
            return caller.To();
        }
    }

    class RouteComponent : IRouteComponent {
        internal static readonly Version MULTI_CHANNEL_MATH= new Version("1.1.0"), MULTI_COMPARISON_MIN_FIRMWARE= new Version("1.2.3");

        internal class State {
            internal readonly List<Tuple<DataTypeBase, Action<IData>, bool, string>> subscribedProducers = new List<Tuple<DataTypeBase, Action<IData>, bool, string>>();
            internal readonly Stack<Tuple<RouteComponent, BranchType>> stashedComponents = new Stack<Tuple<RouteComponent, BranchType>>();
            internal readonly Stack<Tuple<RouteComponent, DataTypeBase[]>> splits = new Stack<Tuple<RouteComponent, DataTypeBase[]>>();
            internal readonly LinkedList<Tuple<DataTypeBase, EditorImplBase>> dataProcessors = new LinkedList<Tuple<DataTypeBase, EditorImplBase>>();
            internal readonly Dictionary<string, Tuple<DataTypeBase, EditorImplBase>> namedProcessors = new Dictionary<string, Tuple<DataTypeBase, EditorImplBase>>();
            internal readonly List<Tuple<DataTypeBase, Action<IDataToken>>> reactions = new List<Tuple<DataTypeBase, Action<IDataToken>>>();
            internal readonly List<Tuple<string, DataTypeBase, byte, byte[]>> feedback = new List<Tuple<string, DataTypeBase, byte, byte[]>>();
            internal readonly IModuleBoardBridge bridge;
            internal bool lastestWasSubscriber = false;

            internal State(IModuleBoardBridge bridge) {
                this.bridge = bridge;
            }
        }

        internal DataTypeBase source;
        internal State state;

        public RouteComponent(DataTypeBase source, IModuleBoardBridge bridge) {
            this.source = source;
            state = new State(bridge);
        }

        public RouteComponent(DataTypeBase source, RouteComponent original) {
            this.source = source;
            state = original.state;
        }

        public IRouteComponent Stream(Action<IData> subscriber) {
            if (source.attributes.length() > 0) {
                state.lastestWasSubscriber = true;
                source.markLive();
                state.subscribedProducers.Add(Tuple.Create(source, subscriber, false, ""));
                return this;
            }
            throw new IllegalRouteOperationException("Cannot stream null data");
        }
        public IRouteComponent Stream() {
            return Stream(null);
        }

        public IRouteComponent Log(Action<IData> subscriber) {
            if (state.bridge.GetModule<ILogging>() == null) {
                throw new IllegalRouteOperationException("Offline logging not supported on this board / firmware");
            }
            if (source.attributes.length() > 0) {
                state.lastestWasSubscriber = true;
                state.subscribedProducers.Add(Tuple.Create(source, subscriber, true, ""));
                return this;
            }
            throw new IllegalRouteOperationException("Cannot log null data");
        }
        public IRouteComponent Log() {
            return Log(null);
        }

        public IRouteComponent React(Action<IDataToken> action) {
            if (state.bridge.GetModule<Event>() == null) {
                throw new IllegalRouteOperationException("Event handling not supported on this board / firmware");
            }
            state.reactions.Add(Tuple.Create(source, action));
            return this;
        }

        public IRouteComponent Name(string name) {
            if (state.namedProcessors.ContainsKey(name)) {
                throw new IllegalRouteOperationException(string.Format("Duplicate processor name found in route: '{0}'", name));
            }
            if (!state.lastestWasSubscriber) {
                if (state.dataProcessors.Count != 0) {
                    state.namedProcessors.Add(name, state.dataProcessors.Last.Value);
                } else {
                    state.bridge.registerProducerName(name, source);
                }
            } else {
                var last = state.subscribedProducers[state.subscribedProducers.Count - 1];
                state.subscribedProducers[state.subscribedProducers.Count - 1] = Tuple.Create(last.Item1, last.Item2, last.Item3, name);
            }
            return this;
        }

        public IRouteMulticast Multicast() {
            state.stashedComponents.Push(Tuple.Create(this, BranchType.Multicast));
            return new RouteMulticast(this);
        }

        public IRouteComponent To() {
            if (state.stashedComponents.Count == 0) {
                throw new IllegalRouteOperationException("No branch detected");
            }
            if (state.stashedComponents.Peek().Item2 != BranchType.Multicast) {
                throw new IllegalRouteOperationException("Most recent branch is a split component");
            }
            return state.stashedComponents.Peek().Item1;
        }

        public IRouteSplit Split() {
            if (source.components == null) {
                throw new IllegalRouteOperationException(string.Format("Cannot split source data signal '{0}'", source.GetType()));
            }

            state.stashedComponents.Push(Tuple.Create(this, BranchType.Split));
            state.splits.Push(Tuple.Create(this, source.components));
            return new RouteSplit(this);
        }

        public IRouteComponent Index(int i) {
            if (state.stashedComponents.Count == 0) {
                throw new IllegalRouteOperationException("No branch detected");
            }
            if (state.stashedComponents.Peek().Item2 != BranchType.Split) {
                throw new IllegalRouteOperationException("Most recent branch is a multicast component");
            }

            try {
                return new RouteComponent(state.splits.Peek().Item2[i], this);
            } catch (IndexOutOfRangeException e) {
                throw new IllegalRouteOperationException("Index on split data out of bounds", e);
            }
        }

        [DataContract]
        internal class AverageEditorInner : EditorImplBase, IHighPassEditor, ILowPassEditor {
            internal AverageEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }
            
            public void Modify(byte samples) {
                config[2] = samples;
                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }

            public void Reset() {
                bridge.sendCommand(new byte[] { (byte) DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2] });
            }
        }

        private IRouteComponent applyAverager(byte samples, bool hpf, string name) {
            var hasHpf = state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision >= DataProcessor.HPF_REVISION;

            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException(string.Format("Cannot apply {0} filter to null data", name));
            }
            if (source.attributes.length() > 4 && !hasHpf) {
                throw new IllegalRouteOperationException(string.Format("Cannot apply {0} filter to data longer than 4 bytes", name));
            }

            DataProcessorConfig config = new DataProcessorConfig.AverageConfig(source.attributes, samples, hpf, hasHpf);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);
            
            return postCreate(next.Item2, new AverageEditorInner(config, next.Item1, state.bridge));
        }
        public IRouteComponent HighPass(byte samples) {
            if (state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision < DataProcessor.HPF_REVISION) {
                throw new IllegalRouteOperationException("High pass filter not available on this firmware version");
            }
            return applyAverager(samples, true, "high-pass");
        }
        public IRouteComponent LowPass(byte samples) {
            return applyAverager(samples, false, "low-pass");
        }
        public IRouteComponent Average(byte samples) {
            return LowPass(samples);
        }

        [DataContract]
        internal class CounterEditorInner : EditorImplBase, ICounterEditor {
            internal CounterEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Reset() {
                bridge.sendCommand(new byte[] {(byte) DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2],
                        0x00, 0x00, 0x00, 0x00});
            }

            public void Set(uint value) {
                byte[] command = new byte[7];
                command[0] = (byte)DATA_PROCESSOR;
                command[1] = DataProcessor.STATE;
                command[2] = source.eventConfig[2];
                Array.Copy(Util.uintToBytesLe(value), 0, command, 3, 4);

                bridge.sendCommand(command);
            }
        }

        [DataContract]
        internal class AccumulatorEditorInner : EditorImplBase, IAccumulatorEditor {
            internal AccumulatorEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Reset() {
                bridge.sendCommand(new byte[] {(byte) DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2],
                        0x00, 0x00, 0x00, 0x00});
            }

            public void Set(float value) {
                byte[] command = new byte[7];
                command[0] = (byte)DATA_PROCESSOR;
                command[1] = DataProcessor.STATE;
                command[2] = source.eventConfig[2];
                Array.Copy(Util.intToBytesLe((int)(source.scale(bridge) * value)), 0, command, 3, 4);

                bridge.sendCommand(command);
            }
        }

        public IRouteComponent Accumulate() {
            return createReducer(false);
        }
        public IRouteComponent Count() {
            return createReducer(true);
        }
        private RouteComponent createReducer(bool counter) {
            if (!counter) {
                if (source.attributes.length() <= 0) {
                    throw new IllegalRouteOperationException("Cannot accumulate null data");
                }
                if (source.attributes.length() > 4) {
                    throw new IllegalRouteOperationException("Cannot accumulate data longer than 4 bytes");
                }
            }

            byte output = 4;
            DataProcessorConfig config = new AccumulatorConfig(counter, output, source.attributes.length());
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);
            
            EditorImplBase editor = counter ?
                new CounterEditorInner(config, next.Item1, state.bridge) as EditorImplBase :
                new AccumulatorEditorInner(config, next.Item1, state.bridge) as EditorImplBase;

            return postCreate(next.Item2, editor);
        }

        public IRouteBranchEnd Buffer() {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot buffer null data");
            }

            DataTypeBase processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { }, 0, 0, false));
            var config = new BufferConfig(source.attributes.length());

            state.dataProcessors.AddLast(Tuple.Create(
                source.dataProcessorStateCopy(source, source.attributes), 
                new NullEditor(config, processor, state.bridge) as EditorImplBase
            ));
            return new RouteBranchEnd(this);
        }

        [DataContract]
        internal class SingleValueComparatorEditor : EditorImplBase, IComparatorEditor {
            internal SingleValueComparatorEditor(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : 
                base(config, source, bridge) {
            }

            public void Modify(Comparison op, params float[] references) {
                byte[] newConfig = new byte[6];
                newConfig[0] = (byte)op;
                newConfig[1] = 0;
                Array.Copy(Util.intToBytesLe((int)(references[0] * source.scale(bridge))), 0, newConfig, 2, 4);
                
                Array.Copy(newConfig, 0, config, 2, newConfig.Length);
                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }
        }
        [DataContract]
        internal class MultiValueComparatorEditor : EditorImplBase, IComparatorEditor {
            internal static byte[] fillReferences(float scale, DataTypeBase source, params float[] references) {
                byte[] buffer = new byte[references.Length * source.attributes.length()];
                int offset = 0;

                switch (source.attributes.length()) {
                    case 1:
                        foreach (float it in references) {
                            buffer[offset] = (byte)(scale * it);
                            offset++;
                        }
                        break;
                    case 2:
                        if (source.attributes.signed) {
                            foreach (float it in references) {
                                Array.Copy(Util.shortToBytesLe((short)(scale * it)), 0, buffer, offset, 2);
                                offset += 2;
                            }
                        } else {
                            foreach (float it in references) {
                                Array.Copy(Util.ushortToBytesLe((ushort)(scale * it)), 0, buffer, offset, 2);
                                offset += 2;
                            }
                        }
                        break;
                    case 4:
                        if (source.attributes.signed) {
                            foreach (float it in references) {
                                Array.Copy(Util.intToBytesLe((int)(scale * it)), 0, buffer, offset, 4);
                                offset += 4;
                            }
                        } else {
                            foreach (float it in references) {
                                Array.Copy(Util.uintToBytesLe((uint)(scale * it)), 0, buffer, offset, 4);
                                offset += 4;
                            }
                        }
                        break;
                }
                return buffer;
            }

            internal MultiValueComparatorEditor(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Modify(Comparison op, params float[] references) {
                byte[] newRef = fillReferences(source.scale(bridge), source, references);

                byte[] newConfig = new byte[2 + references.Length * source.attributes.length()];
                newConfig[0] = config[0];
                newConfig[1] = (byte)((config[1] & ~0x38) | ((int) op << 3));

                Array.Copy(newRef, 0, newConfig, 2, newRef.Length);
                config = newConfig;

                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }
        }
        public IRouteComponent Filter(Comparison op, params float[] references) {
            return Filter(op, ComparisonOutput.Absolute, references);
        }
        public IRouteComponent Filter(Comparison op, ComparisonOutput output, params float[] references) {
            if (source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot compare data longer than 4 bytes");
            }

            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot compare null data");
            }

            if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException("Cannot compare sensor sensor fusion data");
            }

            if (state.bridge.getFirmware().CompareTo(MULTI_COMPARISON_MIN_FIRMWARE) < 0) {
                float scaledReference = references[0] * source.scale(state.bridge);
                DataProcessorConfig config = new SingleValueComparisonConfig(source.attributes.signed, op, (int)scaledReference);
                var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);
                    
                return postCreate(next.Item2, new SingleValueComparatorEditor(config, next.Item1, state.bridge));
            }

            bool anySigned = false;
            foreach (float it in references) {
                anySigned |= it < 0;
            }
            bool signed = source.attributes.signed || anySigned;

            {
                DataProcessorConfig config = new MultiValueComparisonConfig(signed, source.attributes.length(), op, output,
                    MultiValueComparatorEditor.fillReferences(source.scale(state.bridge), source, references));
                var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

                return postCreate(next.Item2, new MultiValueComparatorEditor(config, next.Item1, state.bridge));
            }
        }
        public IRouteComponent Filter(Comparison op, params string[] names) {
            return Filter(op, ComparisonOutput.Absolute, names);
        }
        public IRouteComponent Filter(Comparison op, ComparisonOutput output, params string[] names) {
            RouteComponent next = Filter(op, output, 0) as RouteComponent;
            if (next != null) {
                foreach (string it in names) {
                    byte dest = (byte) (state.bridge.getFirmware().CompareTo(MULTI_COMPARISON_MIN_FIRMWARE) < 0 ? 5 : 3);
                    state.feedback.Add(Tuple.Create(it, next.source, dest, state.dataProcessors.Last.Value.Item2.config));
                }
            }

            return next;
        }

        public IRouteComponent Map(Function1 fn) {
            switch (fn) {
                case Function1.AbsValue:
                    return applyMath(Operation.AbsValue, 0);
                case Function1.Rms:
                    if (source is FloatVectorDataType) {
                        return createCombiner(source, false);
                    }
                    throw new IllegalRouteOperationException("Cannot map data to RMS function");
                case Function1.Rss:
                    if (source is FloatVectorDataType) {
                        return createCombiner(source, true);
                    }
                    throw new IllegalRouteOperationException("Cannot map data to RSS function");
                case Function1.Sqrt:
                    return applyMath(Operation.Sqrt, 0);
            }
            throw new Exception("Just here so compiler doesn't complain");
        }

        public IRouteComponent Map(Function2 fn, float rhs) {
            switch (fn) {
                case Function2.Add:
                    return applyMath(Operation.Add, rhs);
                case Function2.Multiply:
                    return applyMath(Operation.Multiply, rhs);
                case Function2.Divide:
                    return applyMath(Operation.Divide, rhs);
                case Function2.Modulus:
                    return applyMath(Operation.Modulus, rhs);
                case Function2.Exponent:
                    return applyMath(Operation.Exponent, rhs);
                case Function2.LeftShift:
                    return applyMath(Operation.LeftShift, rhs);
                case Function2.RightShift:
                    return applyMath(Operation.RightShift, rhs);
                case Function2.Subtract:
                    return applyMath(Operation.Subtract, rhs);
                case Function2.Constant:
                    return applyMath(Operation.Constant, rhs);
            }
            throw new Exception("Only here so the compiler won't get mad");
        }

        public IRouteComponent Map(Function2 fn, params string[] names) {
            RouteComponent next = Map(fn, 0) as RouteComponent;
            if (next != null) {
                foreach (string it in names) {
                    state.feedback.Add(Tuple.Create(it, next.source, (byte) 4, state.dataProcessors.Last.Value.Item2.config));
                }
            }
            return next;
        }

        [DataContract]
        internal class MapEditorInner : EditorImplBase, IMapEditor {
            internal MapEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void ModifyRhs(float rhs) {
                float scaledRhs;

                switch ((Operation) (config[2] - 1)) {
                    case Operation.Add:
                    case Operation.Modulus:
                    case Operation.Subtract:
                        scaledRhs = rhs * source.scale(bridge);
                        break;
                    case Operation.Sqrt:
                    case Operation.AbsValue:
                        scaledRhs = 0;
                        break;
                    default:
                        scaledRhs = rhs;
                        break;
                }

                byte[] newRhs = Util.intToBytesLe((int)scaledRhs);
                Array.Copy(newRhs, 0, config, 3, newRhs.Length);
                
                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }
        }
        private RouteComponent applyMath(Operation op, float rhs) {
            bool multiChnlMath = state.bridge.getFirmware().CompareTo(MULTI_CHANNEL_MATH) >= 0;

            if (!multiChnlMath && source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot apply math operations on multi-channel data for firmware prior to " + MULTI_CHANNEL_MATH.ToString());
            }

            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot apply math operations to null data");
            }

            if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException("Cannot apply math operations to sensor fusion data");
            }

            int scaledRhs;
            switch (op) {
                case Operation.Add:
                case Operation.Modulus:
                case Operation.Subtract:
                    scaledRhs = (int)(rhs * source.scale(state.bridge));
                    break;
                case Operation.Sqrt:
                case Operation.AbsValue:
                    scaledRhs = 0;
                    break;
                default:
                    scaledRhs = (int) rhs;
                    break;
            }

            var config = new MathConfig(source.attributes, multiChnlMath, op, scaledRhs);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);
            config.output = next.Item1.attributes.sizes[0];

            return postCreate(next.Item2, new MapEditorInner(config, next.Item1, state.bridge));
        }

        private RouteComponent createCombiner(DataTypeBase source, bool rss) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException(string.Format("Cannot apply \'{0}\' to null data", !rss ? "rms" : "rss"));
            } else if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException(string.Format("Cannot apply \'{0}\' to sensor fusion data", !rss ? "rms" : "rss"));
            }

            var config = new CombinerConfig(source.attributes, rss);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new NullEditor(config, next.Item1, state.bridge));
        }

        [DataContract]
        internal class PassthroughEditorInner : EditorImplBase, IPassthroughEditor {
            internal PassthroughEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge)  : base(config, source, bridge) { }

            public void Set(ushort value) {
                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2], Util.ushortToBytesLe(value));
            }

            public void Modify(Passthrough type, ushort value) {
                Array.Copy(Util.ushortToBytesLe(value), 0, config, 2, 2);
                config[1] = (byte) (((int) type) & 0x7);

                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }
        }

        public IRouteComponent Limit(Passthrough type, ushort value) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot limit null data");
            }

            var config = new PassthroughConfig(type, value);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new PassthroughEditorInner(config, next.Item1, state.bridge));
        }

        public IRouteComponent Delay(byte samples) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot delay null data");
            }

            bool enhanced = state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision >= DataProcessor.EXPANDED_DELAY;
            int maxLength = enhanced ? 16 : 4;
            if (source.attributes.length() > maxLength) {
                throw new IllegalRouteOperationException(string.Format("Firmware does not support delayed data longer than {0} bytes", maxLength));
            }

            var config = new DelayConfig(enhanced, source.attributes.length(), samples);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new NullEditor(config, next.Item1, state.bridge));
        }

        [DataContract]
        internal class PulseEditorInner : EditorImplBase, IPulseEditor {
            internal PulseEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Modify(float threshold, ushort samples) {
                byte[] newConfig = new byte[6];
                Array.Copy(Util.intToBytesLe((int) (threshold * source.scale(bridge))), newConfig, 4);
                Array.Copy(Util.ushortToBytesLe(samples), 0, newConfig, 4, 2);

                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, newConfig);
            }
        }
        public IRouteComponent Find(Pulse pulse, float threshold, ushort samples) {
            if (source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot find pulses for data longer than 4 bytes");
            }

            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot find pulses for null data");
            }

            if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException("Cannot find pulses for sensor fusion data");
            }

            var config = new PulseConfig(source.attributes.length(), (int) (threshold * source.scale(state.bridge)), samples, pulse);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new PulseEditorInner(config, next.Item1, state.bridge));
        }

        [DataContract]
        internal class ThresholdEditorInner : EditorImplBase, IThresholdEditor {
            internal ThresholdEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Modify(float threshold, float hysteresis) {
                byte[] newConfig = new byte[6];
                Array.Copy(Util.intToBytesLe((int) (source.scale(bridge) * threshold)), newConfig, 4);
                Array.Copy(Util.shortToBytesLe((short)(source.scale(bridge) * hysteresis)), 4, newConfig, 0, 2);

                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, newConfig);
            }
        }
        public IRouteComponent Find(Threshold threshold, float boundary) {
            return Find(threshold, boundary, 0);
        }

        public IRouteComponent Find(Threshold threshold, float boundary, float hysteresis) {
            if (source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot use threshold filter on data longer than 4 bytes");
            }

            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot use threshold filter on null data");
            }

            if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException("Cannot use threshold filter on sensor fusion data");
            }

            var config = new ThresholdConfig(source.attributes, threshold, (int)(boundary * source.scale(state.bridge)), (short)(hysteresis * source.scale(state.bridge)));
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new ThresholdEditorInner(config, next.Item1, state.bridge));
        }

        [DataContract]
        internal class DifferentialEditorInner : EditorImplBase, IDifferentialEditor {
            internal DifferentialEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Modify(float distance) {
                Array.Copy(Util.intToBytesLe((int) (distance * source.scale(bridge))), 0, config, 2, 4);

                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }
        }
        public IRouteComponent Find(Differential differential, float distance) {
            if (source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot use differential filter for data longer than 4 bytes");
            }

            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot use differential filter for null data");
            }

            if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException("Cannot use differential filter on sensor fusion data");
            }

            var config = new DifferentialConfig(source.attributes, differential, (int)(distance * source.scale(state.bridge)));
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new DifferentialEditorInner(config, next.Item1, state.bridge));
        }

        [DataContract]
        internal class TimeEditorInner : EditorImplBase, ITimeEditor {
            internal TimeEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Modify(uint period) {
                Array.Copy(Util.uintToBytesLe(period), 0, config, 2, 4);
                
                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }
        }
        public IRouteComponent Limit(uint period) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot limit frequency of null data");
            }

            bool hasTimePassthrough = state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision >= DataProcessor.TIME_PASSTHROUGH_REVISION;
            if (!hasTimePassthrough && source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException("Cannot limit frequency of sensor fusion data");
            }

            var config = new TimeConfig(source.attributes.length(), (byte) (hasTimePassthrough ? 2 : 0), period);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new TimeEditorInner(config, next.Item1, state.bridge));
        }

        [DataContract]
        internal class PackerEditorInner : EditorImplBase, IPackerEditor {
            internal PackerEditorInner(DataProcessorConfig config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Clear() {
                bridge.sendCommand(new byte[] { (byte) DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2] });
            }
        }
        public IRouteComponent Pack(byte count) {
            if (state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision < DataProcessor.ENHANCED_STREAMING_REVISION) {
                throw new IllegalRouteOperationException("Current firmware does not support data packing");
            }
            if (source.attributes.length() * count + 3 > MetaWearBoard.MAX_PACKET_LENGTH) {
                throw new IllegalRouteOperationException("Not enough space to in the ble packet to pack " + count + " data samples");
            }

            var config = new PackerConfig(source.attributes.length(), count);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new PackerEditorInner(config, next.Item1, state.bridge));
        }

        public IRouteComponent Account() {
            return Account(AccountType.Time);
        }

        public IRouteComponent Account(AccountType type) {
            if (state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision < DataProcessor.ENHANCED_STREAMING_REVISION) {
                throw new IllegalRouteOperationException("Current firmware does not support data accounting");
            }

            byte size = (byte) (type == AccountType.Time ? 4 : Math.Min(4, MetaWearBoard.MAX_PACKET_LENGTH - 3 - source.attributes.length()));
            if (type == AccountType.Time && source.attributes.length() + size + 3 > MetaWearBoard.MAX_PACKET_LENGTH || type == AccountType.Count && size < 0) {
                throw new IllegalRouteOperationException("Not enough space left in the ble packet to add accounter information");
            }
            
            var config = new AccounterConfig(type, size);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new NullEditor(config, next.Item1, state.bridge));
        }

        public IRouteComponent Fuse(params string[] bufferNames) {
            if (state.bridge.lookupModuleInfo(DATA_PROCESSOR).revision < DataProcessor.FUSE_REVISION) {
                throw new IllegalRouteOperationException("Current firmware does not support data fusing");
            }

            var config = new FuserConfig(bufferNames);
            var next = source.transform(config, state.bridge.GetModule<IDataProcessor>() as DataProcessor);

            return postCreate(next.Item2, new NullEditor(config, next.Item1, state.bridge));
        }

        private RouteComponent postCreate(DataTypeBase processorState, EditorImplBase editor) {
            state.lastestWasSubscriber = false;
            state.dataProcessors.AddLast(Tuple.Create(processorState, editor));
            return new RouteComponent(editor.source, this);
        }
    }
}
