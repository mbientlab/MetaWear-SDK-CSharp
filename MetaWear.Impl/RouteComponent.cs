using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Core.DataProcessor;

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

        public IRouteComponent To() {
            return caller.To();
        }
    }

    class RouteComponent : IRouteComponent {
        private static readonly Version MULTI_CHANNEL_MATH= new Version("1.1.0"), MULTI_COMPARISON_MIN_FIRMWARE= new Version("1.2.3");

        internal class State {
            internal readonly List<Tuple<DataTypeBase, Action<IData>, bool>> subscribedProducers = new List<Tuple<DataTypeBase, Action<IData>, bool>>();
            internal readonly Stack<Tuple<RouteComponent, BranchType>> stashedComponents = new Stack<Tuple<RouteComponent, BranchType>>();
            internal readonly Stack<Tuple<RouteComponent, DataTypeBase[]>> splits = new Stack<Tuple<RouteComponent, DataTypeBase[]>>();
            internal readonly LinkedList<Tuple<DataTypeBase, EditorImplBase>> dataProcessors = new LinkedList<Tuple<DataTypeBase, EditorImplBase>>();
            internal readonly Dictionary<string, Tuple<DataTypeBase, EditorImplBase>> namedProcessors = new Dictionary<string, Tuple<DataTypeBase, EditorImplBase>>();
            internal readonly List<Tuple<DataTypeBase, Action<IDataToken>>> reactions = new List<Tuple<DataTypeBase, Action<IDataToken>>>();
            internal readonly List<Tuple<string, DataTypeBase, byte, byte[]>> feedback = new List<Tuple<string, DataTypeBase, byte, byte[]>>();
            internal readonly IModuleBoardBridge bridge;

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
                source.markLive();
                state.subscribedProducers.Add(Tuple.Create(source, subscriber, false));
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
                state.subscribedProducers.Add(Tuple.Create(source, subscriber, true));
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
            if (state.dataProcessors.Count != 0) {
                state.namedProcessors.Add(name, state.dataProcessors.Last.Value);
            } else {
                state.bridge.registerProducerName(name, source);
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
        internal class CounterEditorInner : EditorImplBase, ICounterEditor {
            internal CounterEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

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
            internal AccumulatorEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void Reset() {
                bridge.sendCommand(new byte[] {(byte) DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2],
                        0x00, 0x00, 0x00, 0x00});
            }

            public void Set(float value) {
                byte[] command = new byte[7];
                command[0] = (byte)DATA_PROCESSOR;
                command[1] = DataProcessor.STATE;
                command[2] = source.eventConfig[2];
                Array.Copy(Util.intToBytesLe((int) (source.scale(bridge) * value)), 0, command, 3, 4);

                bridge.sendCommand(command);
            }
        }

        [DataContract]
        internal class AverageEditorInner : EditorImplBase, IAverageEditor {
            internal AverageEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }
            
            public void Modify(byte samples) {
                config[2] = samples;
                bridge.sendCommand(DATA_PROCESSOR, DataProcessor.PARAMETER, source.eventConfig[2], config);
            }

            public void Reset() {
                bridge.sendCommand(new byte[] { (byte) DATA_PROCESSOR, DataProcessor.STATE, source.eventConfig[2] });
            }
        }

        public IRouteComponent Average(byte samples) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot average null data");
            }
            if (source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot average data longer than 4 bytes");
            }

            DataTypeBase processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
            byte[] config = new byte[]{
                0x3,
                (byte) (((source.attributes.length() - 1) & 0x3) | (((source.attributes.length() - 1) & 0x3) << 2)),
                samples
        };

            return postCreate(null, new AverageEditorInner(config, processor, state.bridge));
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
            DataAttributes attributes = new DataAttributes(new byte[] { output }, 1, 0, !counter && source.attributes.signed);
            DataTypeBase processor = counter ?
                    new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes) :
                    source.dataProcessorCopy(source, attributes);
            byte[] config = new byte[] { 0x2, (byte)(((output - 1) & 0x3) | (((source.attributes.length() - 1) & 0x3) << 2) | (counter ? 0x10 : 0)) };
            EditorImplBase editor = counter ?
                    new CounterEditorInner(config, processor, state.bridge) as EditorImplBase :
                    new AccumulatorEditorInner(config, processor, state.bridge) as EditorImplBase;

            DataTypeBase processorState = counter ?
                    new IntegralDataType(null, DATA_PROCESSOR, Util.setRead(DataProcessor.STATE), attributes) :
                    processor.dataProcessorStateCopy(source, attributes);
            return postCreate(processorState, editor);
        }

        public IRouteBranchEnd Buffer() {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot buffer null data");
            }

            DataTypeBase processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { }, 0, 0, false));
            byte[] config = new byte[] { 0xf, (byte)(source.attributes.length() - 1) };

            state.dataProcessors.AddLast(Tuple.Create(
                source.dataProcessorStateCopy(source, source.attributes), 
                new NullEditor(config, processor, state.bridge) as EditorImplBase
            ));
            return new RouteBranchEnd(this);
        }

        [DataContract]
        internal class SingleValueComparatorEditor : EditorImplBase, IComparatorEditor {
            internal SingleValueComparatorEditor(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : 
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

            internal MultiValueComparatorEditor(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

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

                byte[] config = new byte[8];
                config[0] = 0x6;
                config[1] = (byte)(source.attributes.signed || references[0] < 0 ? 1 : 0);
                config[2] = (byte)op;
                config[3] = 0;
                Array.Copy(Util.intToBytesLe((int)(scaledReference)), 0, config, 4, 4);
                    
                return postCreate(null, new SingleValueComparatorEditor(config, source.dataProcessorCopy(source, source.attributes.dataProcessorCopy()), state.bridge));
            }

            bool anySigned = false;
            foreach (float it in references) {
                anySigned |= it < 0;
            }
            bool signed = source.attributes.signed || anySigned;

            DataTypeBase processor;
            if (output == ComparisonOutput.PassFail || output == ComparisonOutput.Zone) {
                DataAttributes newAttrs = new DataAttributes(new byte[] { 1 }, 1, 0, false);
                processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, newAttrs);
            } else {
                processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
            }

            {
                //scope conflict with 'config' variable name
                byte[] config = new byte[2 + references.Length * source.attributes.length()];
                config[0] = 0x6;
                config[1] = (byte)((signed ? 1 : 0) | ((source.attributes.length() - 1) << 1) | ((int)op << 3) | ((int)output << 6));

                byte[] referenceValues = MultiValueComparatorEditor.fillReferences(source.scale(state.bridge), source, references);
                Array.Copy(referenceValues, 0, config, 2, referenceValues.Length);

                return postCreate(null, new MultiValueComparatorEditor(config, processor, state.bridge));
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

        private enum MathOp {
            Add,
            Multiply,
            Divide,          
            Modulus,            
            Exponent,
            Sqrt,
            LeftShift,
            RightShift,
            Subtract,
            AbsValue,
            Constant
        }

        public IRouteComponent Map(Function1 fn) {
            switch (fn) {
                case Function1.AbsValue:
                    return applyMath(MathOp.AbsValue, 0);
                case Function1.Rms:
                    if (source is FloatVectorDataType) {
                        return createCombiner(source, 0);
                    }
                    throw new IllegalRouteOperationException("Cannot map data to RMS function");
                case Function1.Rss:
                    if (source is FloatVectorDataType) {
                        return createCombiner(source, 1);
                    }
                    throw new IllegalRouteOperationException("Cannot map data to RSS function");
                case Function1.Sqrt:
                    return applyMath(MathOp.Sqrt, 0);
            }
            throw new Exception("Just here so compiler doesn't complain");
        }

        public IRouteComponent Map(Function2 fn, float rhs) {
            switch (fn) {
                case Function2.Add:
                    return applyMath(MathOp.Add, rhs);
                case Function2.Multiply:
                    return applyMath(MathOp.Multiply, rhs);
                case Function2.Divide:
                    return applyMath(MathOp.Divide, rhs);
                case Function2.Modulus:
                    return applyMath(MathOp.Modulus, rhs);
                case Function2.Exponent:
                    return applyMath(MathOp.Exponent, rhs);
                case Function2.LeftShift:
                    return applyMath(MathOp.LeftShift, rhs);
                case Function2.RightShift:
                    return applyMath(MathOp.RightShift, rhs);
                case Function2.Subtract:
                    return applyMath(MathOp.Subtract, rhs);
                case Function2.Constant:
                    return applyMath(MathOp.Constant, rhs);
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
            internal MapEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

            public void ModifyRhs(float rhs) {
                float scaledRhs;

                switch ((MathOp) (config[2] - 1)) {
                    case MathOp.Add:
                    case MathOp.Modulus:
                    case MathOp.Subtract:
                        scaledRhs = rhs * source.scale(bridge);
                        break;
                    case MathOp.Sqrt:
                    case MathOp.AbsValue:
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
        private RouteComponent applyMath(MathOp op, float rhs) {
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

            DataTypeBase processor;

            switch (op) {
                case MathOp.Add: {
                        DataAttributes newAttrs = source.attributes.dataProcessorCopySize(4);
                        newAttrs.signed = source.attributes.signed || (!source.attributes.signed && rhs < 0);

                        processor = source.dataProcessorCopy(source, newAttrs);
                        break;
                    }
                case MathOp.Multiply: {
                        DataAttributes newAttrs = source.attributes.dataProcessorCopySize((byte) (Math.Abs(rhs) < 1 ? source.attributes.sizes[0] : 4));
                        newAttrs.signed = source.attributes.signed || (!source.attributes.signed && rhs < 0);

                        processor = source.dataProcessorCopy(source, newAttrs);
                        break;
                    }
                case MathOp.Divide: {
                        DataAttributes newAttrs = source.attributes.dataProcessorCopySize((byte) (Math.Abs(rhs) < 1 ? 4 : source.attributes.sizes[0]));
                        newAttrs.signed = source.attributes.signed || (!source.attributes.signed && rhs < 0);

                        processor = source.dataProcessorCopy(source, newAttrs);
                        break;
                    }
                case MathOp.Modulus: {
                        processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
                        break;
                    }
                case MathOp.Exponent: {
                        processor = new ByteArrayDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY,
                                source.attributes.dataProcessorCopySize((byte)4));
                        break;
                    }
                case MathOp.LeftShift: {
                        processor = new ByteArrayDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY,
                                source.attributes.dataProcessorCopySize((byte)Math.Min(source.attributes.sizes[0] + ((int) rhs / 8), 4)));
                        break;
                    }
                case MathOp.RightShift: {
                        processor = new ByteArrayDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY,
                                source.attributes.dataProcessorCopySize((byte)Math.Max(source.attributes.sizes[0] - ((int) rhs / 8), 1)));
                        break;
                    }
                case MathOp.Subtract: {
                        processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopySigned(true));
                        break;
                    }
                case MathOp.Sqrt: {
                        processor = new ByteArrayDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, source.attributes.dataProcessorCopySigned(false));
                        break;
                    }
                case MathOp.AbsValue: {
                        DataAttributes copy = source.attributes.dataProcessorCopySigned(false);
                        processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopySigned(false));
                        break;
                    }
                case MathOp.Constant:
                    DataAttributes attributes = new DataAttributes(new byte[] { 4 }, 1, 0, source.attributes.signed);
                    processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes);
                    break;
                default:
                    processor = null;
                    break;
            }

            float scaledRhs;
            switch (op) {
                case MathOp.Add:
                case MathOp.Modulus:
                case MathOp.Subtract:
                    scaledRhs = rhs * source.scale(state.bridge);
                    break;
                case MathOp.Sqrt:
                case MathOp.AbsValue:
                    scaledRhs = 0;
                    break;
                default:
                    scaledRhs = rhs;
                    break;
            }

            byte[] config = new byte[multiChnlMath ? 8 : 7];
            config[0] = 0x9;
            config[1] = (byte)((processor.attributes.sizes[0] - 1) & 0x3 | ((source.attributes.sizes[0] - 1) << 2) | (source.attributes.signed ? 0x10 : 0));
            config[2] = (byte) ((byte)op + 1);
            Array.Copy(Util.intToBytesLe((int)scaledRhs), 0, config, 3, 4);
            
            if (multiChnlMath) {
                config[7] = (byte)(source.attributes.sizes.Length - 1);
            }

            return postCreate(null, new MapEditorInner(config, processor, state.bridge));
        }

        private RouteComponent createCombiner(DataTypeBase source, byte mode) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException(string.Format("Cannot apply \'{0}\' to null data", mode == 0 ? "rms" : "rss"));
            } else if (source.eventConfig[0] == (byte) SENSOR_FUSION) {
                throw new IllegalRouteOperationException(string.Format("Cannot apply \'{0}\' to sensor fusion data", mode == 0 ? "rms" : "rss"));
            }

            byte signedMask = (byte)(source.attributes.signed ? 0x80 : 0x0);
            // assume sizes array is filled with the same value
            DataAttributes attributes = new DataAttributes(new byte[] { source.attributes.sizes[0] }, 1, 0, false);
            DataTypeBase processor = source is FloatVectorDataType ?
                new FloatDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes) as DataTypeBase :
                new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes) as DataTypeBase;
            byte[] config = new byte[] {
                0x7,
                (byte) (((processor.attributes.sizes[0] - 1) & 0x3) | (((source.attributes.sizes[0] - 1) & 0x3) << 2) | (((source.attributes.sizes.Length - 1) & 0x3) << 4) | signedMask),
                mode
            };

            return postCreate(null, new NullEditor(config, processor, state.bridge));
        }

        [DataContract]
        internal class PassthroughEditorInner : EditorImplBase, IPassthroughEditor {
            internal PassthroughEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge)  : base(config, source, bridge) { }

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

            DataTypeBase processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
            byte[] config = new byte[4] { 0x1, (byte) (((int)type) & 0x7), 0, 0 };
            Array.Copy(Util.ushortToBytesLe(value), 0, config, 2, 2);

            DataTypeBase processorState = new IntegralDataType(DATA_PROCESSOR, Util.setRead(DataProcessor.STATE),
                    new DataAttributes(new byte[] { 2 }, (byte)1, (byte)0, false));
            return postCreate(processorState, new PassthroughEditorInner(config, processor, state.bridge));
        }

        public IRouteComponent Delay(byte samples) {
            if (source.attributes.length() <= 0) {
                throw new IllegalRouteOperationException("Cannot delay null data");
            }
            if (source.attributes.length() > 4) {
                throw new IllegalRouteOperationException("Cannot delay data longer than 4 bytes");
            }

            DataTypeBase processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
            byte[] config = new byte[] { 0xa, (byte)((source.attributes.length() - 1) & 0x3), samples };

            return postCreate(null, new NullEditor(config, processor, state.bridge));
        }

        [DataContract]
        internal class PulseEditorInner : EditorImplBase, IPulseEditor {
            internal PulseEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

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

            DataTypeBase processor;

            switch (pulse) {
                case Pulse.Width:
                    processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 2 }, 1, 0, false));
                    break;
                case Pulse.Area:
                    processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopySize((byte)4));
                    break;
                case Pulse.Peak:
                    processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
                    break;
                case Pulse.OnDetect:
                    processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 1 }, 1, 0, false));
                    break;
                default:
                    processor = null;
                    break;
            }

            byte[] config = new byte[10];
            config[0] = 0xb;
            config[1] = (byte) (source.attributes.length() - 1);
            config[2] = 0x0;
            config[3] = (byte)pulse;
            Array.Copy(Util.intToBytesLe((int) (threshold * source.scale(state.bridge))), 0, config, 4, 4);
            Array.Copy(Util.ushortToBytesLe(samples), 0, config, 8, 2);

            return postCreate(null, new PulseEditorInner(config, processor, state.bridge));
        }

        [DataContract]
        internal class ThresholdEditorInner : EditorImplBase, IThresholdEditor {
            internal ThresholdEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

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

            DataTypeBase processor;
            switch (threshold) {
                case Threshold.Absolute:
                    processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
                    break;
                case Threshold.Binary:
                    processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 1 }, 1, 0, true));
                    break;
                default:
                    processor = null;
                    break;
            }

            byte[] config = new byte[8];
            config[0] = 0xd;
            config[1] = (byte)((source.attributes.length() - 1) & 0x3 | (source.attributes.signed ? 0x4 : 0) | ((byte)threshold << 3));
            Array.Copy(Util.intToBytesLe((int) (source.scale(state.bridge) * boundary)), 0, config, 2, 4);
            Array.Copy(Util.shortToBytesLe((short)(source.scale(state.bridge) * hysteresis)), 0, config, 6, 2);

            return postCreate(null, new ThresholdEditorInner(config, processor, state.bridge));
        }

        [DataContract]
        internal class DifferentialEditorInner : EditorImplBase, IDifferentialEditor {
            internal DifferentialEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

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

            DataTypeBase processor;
            switch (differential) {
                case Differential.Absolute:
                    processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());
                    break;
                case Differential.Differential:
                    processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopySigned(true));
                    break;
                case Differential.Binary:
                    processor = new IntegralDataType(source, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 1 }, 1, 0, true));
                    break;
                default:
                    processor = null;
                    break;
            }

            byte[] config = new byte[6];
            config[0] = 0xc;
            config[1] = (byte)(((source.attributes.length() - 1) & 0x3) | (source.attributes.signed ? 0x4 : 0) | ((byte) differential << 3));
            Array.Copy(Util.intToBytesLe((int) (distance * source.scale(state.bridge))), 0, config, 2, 4);

            return postCreate(null, new DifferentialEditorInner(config, processor, state.bridge));
        }

        [DataContract]
        internal class TimeEditorInner : EditorImplBase, ITimeEditor {
            internal TimeEditorInner(byte[] config, DataTypeBase source, IModuleBoardBridge bridge) : base(config, source, bridge) { }

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

            int outputMask = hasTimePassthrough ? 2 : 0;
            DataTypeBase processor = source.dataProcessorCopy(source, source.attributes.dataProcessorCopy());

            byte[] config = new byte[6];
            config[0] = 0x8;
            config[1] = (byte)((source.attributes.length() - 1) & 0x7 | (outputMask << 3));
            Array.Copy(Util.uintToBytesLe(period), 0, config, 2, 4);
            
            return postCreate(null, new TimeEditorInner(config, processor, state.bridge));
        }

        private RouteComponent postCreate(DataTypeBase processorState, EditorImplBase editor) {
            state.dataProcessors.AddLast(Tuple.Create(processorState, editor));
            return new RouteComponent(editor.source, this);
        }        
    }
}
