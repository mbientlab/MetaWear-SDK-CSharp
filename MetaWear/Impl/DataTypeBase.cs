using static MbientLab.MetaWear.Builder.Pulse;
using static MbientLab.MetaWear.Impl.Module;
using static MbientLab.MetaWear.Impl.DataProcessorConfig.MathConfig.Operation;

using System;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Sensor;
using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core;
using System.Linq;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(DataAttributes))]
    [KnownType(typeof(DataTypeBase))]
    [DataContract(IsReference = true)]
    abstract class DataTypeBase : IDataToken {
        internal const byte NO_ID = 0xff;

        [DataMember] internal readonly byte[] eventConfig;
        [DataMember] internal readonly DataAttributes attributes;
        [DataMember] internal readonly DataTypeBase input;
        [DataMember] internal readonly DataTypeBase[] components;

        internal DataTypeBase(byte[] eventConfig, byte offset, byte length) {
            this.eventConfig = eventConfig;
            input = null;
            components = null;
            attributes = new DataAttributes(new byte[] { length }, 1, offset, false);
        }

        internal DataTypeBase(Module module, byte register, DataAttributes attributes) :
            this(null, module, register, NO_ID, attributes) { }

        internal DataTypeBase(Module module, byte register, byte id, DataAttributes attributes) :
            this(null, module, register, id, attributes) { }

        internal DataTypeBase(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
            eventConfig = new byte[] { (byte) module, register, id };
            this.attributes = attributes;
            this.input = input;
            components = createSplits();

            markSilent();
        }

        internal DataTypeBase(DataTypeBase input, Module module, byte register, DataAttributes attributes) :
            this(input, module, register, NO_ID, attributes) { }

        public Tuple<byte, byte, byte> eventConfigAsTuple() {
            return Tuple.Create(eventConfig[0], eventConfig[1], eventConfig[2]);
        }

        public void markSilent() {
            if ((eventConfig[1] & 0x80) == 0x80) {
                eventConfig[1] |= 0x40;
            }
        }

        public void markLive() {
            if ((eventConfig[1] & 0x80) == 0x80) {
                eventConfig[1] &= (~0x40 & 0xff);
            }
        }

        public byte[] createReadStateCmd() {
            return eventConfig[2] == NO_ID ? new byte[] { eventConfig[0], Util.setRead(eventConfig[1]) } : 
                new byte[] { eventConfig[0], Util.setRead(eventConfig[1]), eventConfig[2] };
        }

        public void read(IModuleBoardBridge bridge) {
            if (eventConfig[2] == NO_ID) {
                bridge.sendCommand(new byte[] { eventConfig[0], eventConfig[1] });
            } else {
                bridge.sendCommand(eventConfig);
            }
        }

        public void read(IModuleBoardBridge bridge, byte[] parameters) {
            read(bridge, eventConfig[1], parameters);
        }

        public void read(IModuleBoardBridge bridge, byte register, byte[] parameters) {
            int length = (eventConfig[2] == NO_ID ? 2 : 3);
            byte[] cmd = new byte[parameters.Length + length];
            Array.Copy(eventConfig, 0, cmd, 0, length);
            Array.Copy(parameters, 0, cmd, length, parameters.Length);
            cmd[1] = register;

            bridge.sendCommand(cmd);
         }

        public virtual float scale(IModuleBoardBridge bridge) {
            return (input == null) ? 1f : input.scale(bridge);
        }

        public abstract DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes);
        public DataTypeBase dataProcessorCopy(DataTypeBase input, DataAttributes attributes) {
            return copy(input, DATA_PROCESSOR, DataProcessor.NOTIFY, NO_ID, attributes);
        }
        public DataTypeBase dataProcessorStateCopy(DataTypeBase input, DataAttributes attributes) {
            return copy(input, DATA_PROCESSOR, Util.setRead(DataProcessor.STATE), NO_ID, attributes);
        }

        public abstract DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp);
        protected virtual DataTypeBase[] createSplits() {
            return null;
        }

        internal string CreateIdentifier(IModuleBoardBridge bridge) {
            string myIdentifier = null;
            switch((Module) eventConfig[0]) {
                case SWITCH:
                    myIdentifier = "switch";
                    break;
                case ACCELEROMETER: {
                    var module = bridge.GetModule<IAccelerometer>();
                    if (module is AccelerometerMma8452q) {
                        myIdentifier = AccelerometerMma8452q.createIdentifier(this);
                    } else if (module is AccelerometerBmi160) {
                        myIdentifier = AccelerometerBmi160.createIdentifier(this);
                    } else if (module is AccelerometerBma255) {
                        myIdentifier = AccelerometerBosch.createIdentifier(this);
                    } else {
                        myIdentifier = null;
                    }
                    break;
                }
                case TEMPERATURE:
                    myIdentifier = string.Format("temperature[{0}]", eventConfig[2]);
                    break;
                case GPIO:
                    myIdentifier = Gpio.createIdentifier(this);
                    break;
                case DATA_PROCESSOR:
                    myIdentifier = DataProcessor.createIdentifier(this, bridge.GetModule<IDataProcessor>() as DataProcessor, bridge.getFirmware(), bridge.lookupModuleInfo(DATA_PROCESSOR).revision);
                    break;
                case SERIAL_PASSTHROUGH:
                    myIdentifier = SerialPassthrough.createIdentifier(this);
                    break;
                case SETTINGS:
                    myIdentifier = Settings.createIdentifier(this);
                    break;
                case BAROMETER:
                    myIdentifier = BarometerBosch.createIdentifier(this);
                    break;
                case GYRO:
                    myIdentifier = GyroBmi160.createIdentifier(this);
                    break;
                case AMBIENT_LIGHT:
                    myIdentifier = "illuminance";
                    break;
                case MAGNETOMETER:
                    myIdentifier = MagnetometerBmm150.createIdentifier(this);
                    break;
                case HUMIDITY:
                    myIdentifier = "relative-humidity";
                    break;
                case COLOR_DETECTOR:
                    myIdentifier = attributes.length() > 2 ? "color" : string.Format("color[{0}]", (attributes.offset >> 1));
                    break;
                case PROXIMITY:
                    myIdentifier = "proximity";
                    break;
                case SENSOR_FUSION:
                    myIdentifier = SensorFusionBosch.createIdentifier(this);
                    break;
            }

            if (myIdentifier == null) {
                throw new InvalidOperationException("Cannot create identifier string for data type: " + Util.ArrayToHexString(eventConfig));
            }

            return (input != null && !input.eventConfig.SequenceEqual(eventConfig) ? input.CreateIdentifier(bridge) + ":" : "") + myIdentifier;
        }

        internal virtual Tuple<DataTypeBase, DataTypeBase> transform(DataProcessorConfig config, DataProcessor dpModule) {
            switch (config.id) {
                case DataProcessorConfig.BufferConfig.ID:
                    return Tuple.Create(
                            new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { }, 0, 0, false)) as DataTypeBase,
                            dataProcessorStateCopy(this, this.attributes)
                    );
                case DataProcessorConfig.AccumulatorConfig.ID: {
                        DataProcessorConfig.AccumulatorConfig casted = (DataProcessorConfig.AccumulatorConfig)config;
                        DataAttributes attributes = new DataAttributes(new byte[] { casted.output }, 1, 0, !casted.counter && this.attributes.signed);

                        return Tuple.Create(
                                casted.counter ? new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes) : dataProcessorCopy(this, attributes),
                                casted.counter ? new IntegralDataType(null, DATA_PROCESSOR, Util.setRead(DataProcessor.STATE), NO_ID, attributes) :
                                        dataProcessorStateCopy(this, attributes)
                        );
                    }
                case DataProcessorConfig.AverageConfig.ID:
                case DataProcessorConfig.DelayConfig.ID:
                case DataProcessorConfig.TimeConfig.ID:
                    return Tuple.Create<DataTypeBase, DataTypeBase>(dataProcessorCopy(this, this.attributes.dataProcessorCopy()), null);
                case DataProcessorConfig.PassthroughConfig.ID:
                    return Tuple.Create(
                            dataProcessorCopy(this, this.attributes.dataProcessorCopy()),
                            new IntegralDataType(DATA_PROCESSOR, Util.setRead(DataProcessor.STATE), NO_ID, new DataAttributes(new byte[] { 2 }, 1, 0, false)) as DataTypeBase
                    );
                case DataProcessorConfig.MathConfig.ID: {
                    DataProcessorConfig.MathConfig casted = (DataProcessorConfig.MathConfig)config;
                    DataTypeBase processor = null;
                    switch (casted.op) {
                        case Add:
                            processor = dataProcessorCopy(this, this.attributes.dataProcessorCopySize(4));
                            break;
                        case Multiply:
                            processor = dataProcessorCopy(this, this.attributes.dataProcessorCopySize(Math.Abs(casted.rhs) < 1 ? this.attributes.sizes[0] : (byte) 4));
                            break;
                        case Divide:
                            processor = dataProcessorCopy(this, this.attributes.dataProcessorCopySize(Math.Abs(casted.rhs) < 1 ? (byte) 4 : this.attributes.sizes[0]));
                            break;
                        case Subtract:
                            processor = dataProcessorCopy(this, this.attributes.dataProcessorCopySigned(true));
                            break;
                        case AbsValue:
                            processor = dataProcessorCopy(this, this.attributes.dataProcessorCopySigned(false));
                            break;
                        case Modulus:
                            processor = dataProcessorCopy(this, this.attributes.dataProcessorCopy());
                            break;
                        case Exponent:
                            processor = new ByteArrayDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, this.attributes.dataProcessorCopySize(4));
                            break;
                        case LeftShift:
                            processor = new ByteArrayDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY,
                                    this.attributes.dataProcessorCopySize((byte)Math.Min(this.attributes.sizes[0] + (casted.rhs / 8), 4)));
                            break;
                        case RightShift:
                            processor = new ByteArrayDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY,
                                    this.attributes.dataProcessorCopySize((byte)Math.Max(this.attributes.sizes[0] - (casted.rhs / 8), 1)));
                            break;
                        case Sqrt:
                            processor = new ByteArrayDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, this.attributes.dataProcessorCopySigned(false));
                            break;
                        case Constant:
                            DataAttributes attributes = new DataAttributes(new byte[] { 4 }, (byte)1, (byte)0, casted.rhs >= 0);
                            processor = new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, attributes);
                            break;
                    }
                    if (processor != null) {
                        return Tuple.Create<DataTypeBase, DataTypeBase>(processor, null);
                    }
                    break;
                }
                case DataProcessorConfig.PulseConfig.ID: {
                    DataProcessorConfig.PulseConfig casted = (DataProcessorConfig.PulseConfig)config;
                    DataTypeBase processor = null;
                    switch (casted.mode) {
                        case Width:
                            processor = new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 2 }, 1, 0, false));
                            break;
                        case Area:
                            processor = dataProcessorCopy(this, attributes.dataProcessorCopySize(4));
                            break;
                        case Peak:
                            processor = dataProcessorCopy(this, attributes.dataProcessorCopy());
                            break;
                        case OnDetect:
                            processor = new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 1 }, 1, 0, false));
                            break;
                    }
                    if (processor != null) {
                        return Tuple.Create<DataTypeBase, DataTypeBase>(processor, null);
                    }
                    break;
                }
                case DataProcessorConfig.ComparisonConfig.ID: {
                    DataTypeBase processor = null;
                    if (config is DataProcessorConfig.SingleValueComparisonConfig) {
                        processor = dataProcessorCopy(this, attributes.dataProcessorCopy());
                    } else if (config is DataProcessorConfig.MultiValueComparisonConfig) {
                        DataProcessorConfig.MultiValueComparisonConfig casted = (DataProcessorConfig.MultiValueComparisonConfig)config;
                        if (casted.mode == ComparisonOutput.PassFail || casted.mode == ComparisonOutput.Zone) {
                            processor = new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 1 }, 1, 0, false));
                        } else {
                            processor = dataProcessorCopy(this, attributes.dataProcessorCopy());
                        }
                    }
                    if (processor != null) {
                        return Tuple.Create<DataTypeBase, DataTypeBase>(processor, null);
                    }
                    break;
                }
                case DataProcessorConfig.ThresholdConfig.ID: {
                    DataProcessorConfig.ThresholdConfig casted = (DataProcessorConfig.ThresholdConfig)config;
                    switch (casted.mode) {
                        case Threshold.Absolute:
                            return Tuple.Create<DataTypeBase, DataTypeBase>(dataProcessorCopy(this, attributes.dataProcessorCopy()), null);
                        case Threshold.Binary:
                            return Tuple.Create<DataTypeBase, DataTypeBase>(new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY,
                                new DataAttributes(new byte[] { 1 }, 1, 0, true)), null);
                    }
                    break;
                }
                case DataProcessorConfig.DifferentialConfig.ID: {
                    DataProcessorConfig.DifferentialConfig casted = (DataProcessorConfig.DifferentialConfig)config;
                    switch (casted.mode) {
                        case Differential.Absolute:
                            return Tuple.Create<DataTypeBase, DataTypeBase>(dataProcessorCopy(this, attributes.dataProcessorCopy()), null);
                        case Differential.Differential:
                            throw new InvalidOperationException("Differential processor in 'difference' mode must be handled by subclasses");
                        case Differential.Binary:
                            return Tuple.Create<DataTypeBase, DataTypeBase>(new IntegralDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { 1 }, 1, 0, true)), null);
                    }
                    break;
                }
                case DataProcessorConfig.PackerConfig.ID: {
                    DataProcessorConfig.PackerConfig casted = (DataProcessorConfig.PackerConfig)config;
                    return Tuple.Create<DataTypeBase, DataTypeBase>(dataProcessorCopy(this, attributes.dataProcessorCopyCopies(casted.count)), null);
                }
                case DataProcessorConfig.AccounterConfig.ID: {
                    DataProcessorConfig.AccounterConfig casted = (DataProcessorConfig.AccounterConfig)config;
                    return Tuple.Create<DataTypeBase, DataTypeBase>(dataProcessorCopy(this, new DataAttributes(new byte[] { casted.length, attributes.length() }, 1, 0, attributes.signed)), null);
                }
                case DataProcessorConfig.FuserConfig.ID: {
                    byte fusedLength = attributes.length();
                    var casted = config as DataProcessorConfig.FuserConfig;

                    foreach (var _ in casted.filterIds) {
                        fusedLength += dpModule.activeProcessors[_].Item1.attributes.length();
                    }

                        return Tuple.Create<DataTypeBase, DataTypeBase>(new FusedDataType(this, DATA_PROCESSOR, DataProcessor.NOTIFY, new DataAttributes(new byte[] { fusedLength }, 1, 0, attributes.signed)), null);
                }
            }
            throw new InvalidOperationException("Unable to determine the DataTypeBase object for config: " + Util.ArrayToHexString(config.Build()));
        }

        private class SlicedDataToken : DataTypeBase {
            internal SlicedDataToken(byte[] eventConfig, byte offset, byte length) : base(eventConfig, offset, length) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                throw new NotImplementedException();
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                throw new NotImplementedException();
            }
        };

        public IDataToken Slice(byte offset, byte length) {
            if (offset + length > attributes.length()) {
                throw new IndexOutOfRangeException("'offset + length' is greater than data length (" + attributes.length() + ")");
            }

            return new SlicedDataToken(eventConfig, offset, length);
        }
    }
}
