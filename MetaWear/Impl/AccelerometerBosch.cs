using MbientLab.MetaWear.Sensor;
using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using MbientLab.MetaWear.Data;
using MbientLab.MetaWear.Sensor.AccelerometerBosch;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(BoschCartesianFloatData))]
    [KnownType(typeof(BoschOrientationDataType))]
    [KnownType(typeof(BoschFlatDataType))]
    [KnownType(typeof(BoschTapDataType))]
    [KnownType(typeof(BoschLowHighGDataType))]
    [KnownType(typeof(BoschMotionDataType))]
    [DataContract]
    abstract class AccelerometerBosch : ModuleImplBase, IAccelerometerBosch {
        internal static string createIdentifier(DataTypeBase dataType) {
            switch (dataType.eventConfig[1]) {
                case DATA_INTERRUPT:
                    return dataType.attributes.length() > 2 ? "acceleration" : string.Format("acceleration[{0}]", (dataType.attributes.offset >> 1));
                case ORIENT_INTERRUPT:
                    return "orientation";                
                case FLAT_INTERRUPT:
                    return "bosch-flat";
                case LOW_HIGH_G_INTERRUPT:
                    return "bosch-low-high";
                case MOTION_INTERRUPT:
                    return "bosch-motion";
                case TAP_INTERRUPT:
                    return "bosch-tap";
                case PACKED_ACC_DATA:
                    // packed data is handled exactly the same as unpacked so give them the same identifier
                    return "acceleration";
                default:
                    return null;
            }
        }

        private const byte PACKED_ACC_REVISION = 0x1, FLAT_REVISION = 0x2;
        protected const byte POWER_MODE = 1,
            DATA_INTERRUPT_ENABLE = 2, DATA_CONFIG = 3, DATA_INTERRUPT = 4, DATA_INTERRUPT_CONFIG = 5,
            LOW_HIGH_G_INTERRUPT_ENABLE = 0x6, LOW_HIGH_G_CONFIG = 0x7, LOW_HIGH_G_INTERRUPT = 0x8,
            MOTION_INTERRUPT_ENABLE = 0x9, MOTION_CONFIG = 0xa, MOTION_INTERRUPT = 0xb,
            TAP_INTERRUPT_ENABLE = 0xc, TAP_CONFIG = 0xd, TAP_INTERRUPT = 0xe,
            ORIENT_INTERRUPT_ENABLE = 0xf, ORIENT_CONFIG = 0x10, ORIENT_INTERRUPT = 0x11,
            FLAT_INTERRUPT_ENABLE = 0x12, FLAT_CONFIG = 0x13, FLAT_INTERRUPT = 0x14,
            PACKED_ACC_DATA = 0x1c;
        protected const float ORIENT_HYS_G_PER_STEP = 0.0625f, THETA_STEP = (44.8f / 63.0f);

        protected static readonly byte[] RANGE_BIT_MASKS = new byte[] { 0x3, 0x5, 0x8, 0xc };
        internal static readonly float[] RANGES = new float[] { 2f, 4f, 8f, 16f };
        protected static readonly float[] BOSCH_TAP_THS_STEPS = { 0.0625f, 0.125f, 0.250f, 0.5f };
        private const float LOW_THRESHOLD_STEP = 0.00781f, LOW_HYSTERESIS_STEP = 0.125f;
        protected static readonly float[] BOSCH_HIGH_THRESHOLD_STEPS = { 0.00781f, 0.01563f, 0.03125f, 0.0625f },
                BOSCH_HIGH_HYSTERESIS_STEPS = { 0.125f, 0.250f, 0.5f, 1f },
                BOSCH_ANY_MOTION_THS_STEPS = { 0.00391f, 0.00781f, 0.01563f, 0.03125f },
                BOSCH_NO_MOTION_THS_STEPS = BOSCH_ANY_MOTION_THS_STEPS;

        [KnownType(typeof(BoschCartesianFloatData))]
        [KnownType(typeof(FloatDataType))]
        [DataContract(IsReference = true)]
        private class BoschCartesianFloatData : FloatVectorDataType {
            private BoschCartesianFloatData(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                base(input, module, register, id, attributes) { }

            internal BoschCartesianFloatData(byte register, byte copies) :
                base(ACCELEROMETER, register, new DataAttributes(new byte[] { 2, 2, 2 }, copies, 0, true)) { }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschCartesianFloatData(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AccelerationData(bridge, this, timestamp, data);
            }

            public override float scale(IModuleBoardBridge bridge) {
                return (bridge.GetModule<IAccelerometerBosch>() as AccelerometerBosch).DataScale;
            }

            protected override DataTypeBase[] createSplits() {
                return new DataTypeBase[] {
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 0, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 2, true)),
                    new FloatDataType(this, (Module) eventConfig[0], eventConfig[1], eventConfig[2], new DataAttributes(new byte[] { 2 }, 1, 4, true))
                };
            }
        }

        [DataContract]
        private class BoschOrientationDataType : DataTypeBase {
            private class OrientationData : DataBase {
                internal OrientationData(DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                        base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(SensorOrientation) };

                public override T Value<T>() {
                    var type = typeof(T);

                    if (type == typeof(SensorOrientation)) {
                        return (T)Convert.ChangeType((SensorOrientation) (((bytes[0] & 0x6) >> 1) + 4 * ((bytes[0] & 0x8) >> 3)), type);
                    }

                    return base.Value<T>();
                }
            }

            internal BoschOrientationDataType() : base(ACCELEROMETER, ORIENT_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false)) {
            }

            internal BoschOrientationDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                    base(input, module, register, id, attributes) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschOrientationDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new OrientationData(this, bridge, timestamp, data);
            }
        }
        private class OrientationDataProducer : AsyncDataProducerV2, IOrientationDataProducer {
            internal OrientationDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(ORIENT_INTERRUPT_ENABLE, 0x1, dataTypeBase, bridge) {
            }

            public void Configure(OrientationMode? mode = null, float? hysteresis = null) {
                byte[] config = new byte[] { 0x18, 0x48 };

                if (hysteresis.HasValue) {
                    config[0] |= Math.Min((bridge.GetModule<IAccelerometerBosch>() as AccelerometerBosch).MaxOrientHys, (byte)(hysteresis / ORIENT_HYS_G_PER_STEP));
                }

                config[0] &= 0xfc;
                config[0] |= (byte)(mode ?? OrientationMode.Symmetrical);

                bridge.sendCommand(ACCELEROMETER, ORIENT_CONFIG, config);
            }
        }

        [DataContract]
        protected class BoschFlatDataType : DataTypeBase {
            private class FlatData : DataBase {
                internal FlatData(DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                        base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(bool) };

                public override T Value<T>() {
                    int mask = bridge.lookupModuleInfo(ACCELEROMETER).revision >= FLAT_REVISION ? 0x4 : 0x2;
                    
                    var type = typeof(T);
                    if (type == typeof(bool)) {
                        return (T)Convert.ChangeType((bytes[0] & mask) == mask, type);
                    }

                    return base.Value<T>();
                }
            }

            internal BoschFlatDataType() : base(ACCELEROMETER, FLAT_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false)) {
            }

            internal BoschFlatDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                    base(input, module, register, id, attributes) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschFlatDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new FlatData(this, bridge, timestamp, data);
            }
        }
        protected abstract class BoschFlatDataProducer : AsyncDataProducerV2, IFlatDataProducer {
            internal BoschFlatDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(FLAT_INTERRUPT_ENABLE, 0x1, dataTypeBase, bridge) {
            }

            public abstract void Configure(ushort? hold = null, float? theta = null);

            internal void Write(byte hold, float theta) {
                byte[] flatConfig = new byte[] { 0x08, 0x11 };

                flatConfig[0] |= (byte)((int)(theta / THETA_STEP) & 0x3f);
                flatConfig[1] |= (byte)(hold << 4);

                bridge.sendCommand(ACCELEROMETER, FLAT_CONFIG, flatConfig);
            }
        }

        [DataContract]
        private class BoschTapDataType : DataTypeBase {
            private class BoschTapData : DataBase {
                internal BoschTapData(DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                        base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(Tap) };

                public override T Value<T>() {
                    var type = typeof(T);
                    if (type == typeof(Tap)) {
                        TapType? tap = null;
                        if ((bytes[0] & 0x1) == 0x1) {
                            tap = TapType.Double;
                        } else if ((bytes[0] & 0x2) == 0x2) {
                            tap = TapType.Single;
                        }
                        
                        return (T)Convert.ChangeType(new Tap(tap, (bytes[0] & 0x20) == 0x20 ? Sign.Negative : Sign.Positive), type);
                    }

                    return base.Value<T>();
                }
            }

            internal BoschTapDataType() : base(ACCELEROMETER, TAP_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false)) {
            }

            internal BoschTapDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                    base(input, module, register, id, attributes) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschTapDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new BoschTapData(this, bridge, timestamp, data);
            }
        }
        private class BoschTapDataProducer : AsyncDataProducerV2, ITapDataProducer {
            internal BoschTapDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : base(TAP_INTERRUPT_ENABLE, 0x0, dataTypeBase, bridge) {
            }

            public void Configure(bool enableSingle = false, bool enableDouble = false, float? threshold = null, 
                    TapQuietTime? quiet = null, TapShockTime? shock = null, DoubleTapWindow? window = null) {
                byte[] tapConfig = new byte[] { 0x04, 0x0a };

                if (quiet.HasValue) {
                    tapConfig[0] |= (byte) ((int) quiet << 7);
                }

                if (shock.HasValue) {
                    tapConfig[0] |= (byte) ((int) shock << 6);
                }

                if (window.HasValue) {
                    tapConfig[0] |= (byte) window;
                }

                if (threshold.HasValue) {
                    tapConfig[1] &= 0xe0;
                    tapConfig[1] |= Math.Min((byte) 15, (byte) (threshold / BOSCH_TAP_THS_STEPS[(bridge.GetModule<IAccelerometerBosch>() as AccelerometerBosch).DataScaleIndex]));
                }

                mask = 0;
                if (enableSingle) {
                    mask |= 0x2;
                }
                if (enableDouble) {
                    mask |= 0x1;
                }

                bridge.sendCommand(ACCELEROMETER, TAP_CONFIG, tapConfig);
            }

            public override void Stop() {
                mask = 0x3;
                base.Stop();
            }
        }

        [DataContract]
        private class BoschLowHighGDataType : DataTypeBase {
            private class LowHighData : DataBase {
                internal LowHighData(DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                        base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(LowHighG) };

                public override T Value<T>() {
                    var type = typeof(T);
                    if (type == typeof(LowHighG)) {
                        bool CheckHighG(byte axis, byte value) {
                            byte mask = (byte)(0x1 << axis);
                            return (value & mask) == mask;
                        }
                        byte highFirst = (byte)((bytes[0] & 0x1c) >> 2);
                        LowHighG casted = new LowHighG(
                                (bytes[0] & 0x1) == 0x1,
                                (bytes[0] & 0x2) == 0x2,
                                CheckHighG(0, highFirst),
                                CheckHighG(1, highFirst),
                                CheckHighG(2, highFirst),
                                (bytes[0] & 0x20) == 0x20 ? Sign.Negative : Sign.Positive);


                        return (T)Convert.ChangeType(casted, type);
                    }

                    return base.Value<T>();
                }
            }

            internal BoschLowHighGDataType() : base(ACCELEROMETER, LOW_HIGH_G_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false)) {
            }

            internal BoschLowHighGDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                    base(input, module, register, id, attributes) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschLowHighGDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new LowHighData(this, bridge, timestamp, data);
            }
        }
        private class BoschLowHighDDataProducer : AsyncDataProducerV2, ILowAndHighGDataProducer {
            internal BoschLowHighDDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : 
                    base(LOW_HIGH_G_INTERRUPT_ENABLE, 0x0, dataTypeBase, bridge) {
            }

            public void Configure(bool enableLowG = false, ushort? lowDuration = null, float? lowThreshold = null, float? lowHysteresis = null, LowGMode? mode = null, 
                    bool enableHighGx = false, bool enableHighGy = false, bool enableHighGz = false, ushort? highDuration = null, float? highThreshold = null, float? highHysteresis = null) {
                var accelerometer = bridge.GetModule<IAccelerometerBosch>() as AccelerometerBosch;
                byte[] config = accelerometer.InitialLowHighGConfig;

                mask = 0;
                if (enableLowG) {
                    mask |= 0x08;
                }
                if (enableHighGx) {
                    mask |= 0x01;
                }
                if (enableHighGy) {
                    mask |= 0x02;
                }
                if (enableHighGz) {
                    mask |= 0x04;
                }

                if (lowDuration.HasValue) {
                    config[0] = (byte)((lowDuration / accelerometer.LowHighGDurationStep) - 1);
                }
                if (lowThreshold.HasValue) {
                    config[1] = (byte)(lowThreshold / LOW_THRESHOLD_STEP);
                }
                if (highHysteresis.HasValue) {
                    config[2] |= (byte) (((int)(highHysteresis / BOSCH_HIGH_HYSTERESIS_STEPS[accelerometer.DataScaleIndex]) & 0x3) << 6);
                }
                if (mode.HasValue) {
                    config[2] &= 0xfb;
                    config[2] |= (byte) ((byte) mode << 2);
                }
                if (lowHysteresis.HasValue) {
                    config[2] &= 0xfc;
                    config[2] |= (byte) ((byte)(lowHysteresis / LOW_HYSTERESIS_STEP) & 0x3);
                }
                if (highDuration.HasValue) {
                    config[3] = (byte)((highDuration / accelerometer.LowHighGDurationStep) - 1);
                }
                if (highThreshold.HasValue) {
                    config[4] = (byte)(highThreshold / BOSCH_HIGH_THRESHOLD_STEPS[accelerometer.DataScaleIndex]);
                }

                bridge.sendCommand(ACCELEROMETER, LOW_HIGH_G_CONFIG, config);
            }

            public override void Stop() {
                mask = 0xf;
                base.Stop();
            }
        }

        [DataContract]
        protected class BoschMotionDataType : DataTypeBase {
            private class AnyMotionData : DataBase {
                internal AnyMotionData(DataTypeBase datatype, IModuleBoardBridge bridge, DateTime timestamp, byte[] bytes) :
                        base(bridge, datatype, timestamp, bytes) {
                }

                public override Type[] Types => new Type[] { typeof(AnyMotion) };

                public override T Value<T>() {
                    var type = typeof(T);
                    if (type == typeof(AnyMotion)) {
                        bool detected(byte axis, byte value) {
                            byte mask = (byte)(0x1 << (axis + 3));
                            return (value & mask) == mask;
                        }
                        byte highFirst = (byte)((bytes[0] & 0x1c) >> 2);
                        AnyMotion casted = new AnyMotion(
                                (bytes[0] & 0x40) == 0x40 ? Sign.Negative : Sign.Positive,
                                detected(0, bytes[0]),
                                detected(1, bytes[0]),
                                detected(2, bytes[0]));


                        return (T)Convert.ChangeType(casted, type);
                    }

                    return base.Value<T>();
                }
            }

            internal BoschMotionDataType() : base(ACCELEROMETER, MOTION_INTERRUPT, new DataAttributes(new byte[] { 1 }, 1, 0, false)) {
            }

            internal BoschMotionDataType(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) :
                    base(input, module, register, id, attributes) {
            }

            public override DataTypeBase copy(DataTypeBase input, Module module, byte register, byte id, DataAttributes attributes) {
                return new BoschMotionDataType(input, module, register, id, attributes);
            }

            public override DataBase createData(bool logData, IModuleBoardBridge bridge, byte[] data, DateTime timestamp) {
                return new AnyMotionData(this, bridge, timestamp, data);
            }
        }
        protected abstract class BoschMotionDataProducer : AsyncDataProducerV2, IMotionDataProducer {
            protected AccelerometerBosch accelerometer;

            internal BoschMotionDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) :
                    base(MOTION_INTERRUPT_ENABLE, 0x0, dataTypeBase, bridge) {
                accelerometer = bridge.GetModule<IAccelerometerBosch>() as AccelerometerBosch;
            }

            public abstract void ConfigureAny(int? count = null, float? threshold = null);
            protected void ConfigureAnyInner(byte[] config, int? count = null, float? threshold = null) {
                if (count.HasValue) {
                    config[0] &= 0xfc;
                    config[0] |= (byte)(count - 1);
                }

                if (threshold.HasValue) {
                    config[1] = (byte)(threshold / BOSCH_ANY_MOTION_THS_STEPS[accelerometer.DataScaleIndex]);
                }

                mask = 0x7;
                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }


            public abstract void ConfigureNo(int? duration = null, float? threshold = null);

            public abstract void ConfigureSlow(byte? count = null, float? threshold = null);
            protected void ConfigureSlowInner(byte[] config, byte? count = null, float? threshold = null) {
                if (count.HasValue) {
                    config[0] &= 0x3;
                    config[0] |= (byte)((count - 1) << 2);
                }
                if (threshold.HasValue) {
                    config[2] = (byte)(threshold / BOSCH_NO_MOTION_THS_STEPS[accelerometer.DataScaleIndex]);
                }

                mask = 0x38;
                bridge.sendCommand(ACCELEROMETER, MOTION_CONFIG, config);
            }
        }

        [DataMember] private BoschCartesianFloatData accDataType, packedAccDataType;
        [DataMember] private BoschOrientationDataType orientationDataType;
        [DataMember] protected BoschFlatDataType flatDataType;
        [DataMember] private BoschTapDataType tapDataType;
        [DataMember] private BoschLowHighGDataType lowHighDataType;
        [DataMember] protected BoschMotionDataType motionDataType;

        private IAsyncDataProducer acceleration = null, packedAcceleration = null;
        private IOrientationDataProducer orientation = null;
        private ITapDataProducer tap = null;
        private ILowAndHighGDataProducer lowHighG = null;

        public IAsyncDataProducer Acceleration {
            get {
                if (acceleration == null) {
                    acceleration = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, accDataType, bridge);
                }
                return acceleration;
            }
        }
        public IAsyncDataProducer PackedAcceleration {
            get {
                if (bridge.lookupModuleInfo(ACCELEROMETER).revision >= PACKED_ACC_REVISION) {
                    if (packedAcceleration == null) {
                        packedAcceleration = new AsyncDataProducerV2(DATA_INTERRUPT_ENABLE, 0x1, packedAccDataType, bridge);
                    }
                    return packedAcceleration;
                }
                return null;
            }
        }
        public IOrientationDataProducer Orientation {
            get {
                if (orientation == null) {
                    orientation = new OrientationDataProducer(orientationDataType, bridge);
                }
                return orientation;
            }
        }
        public ITapDataProducer Tap {
            get {
                if (tap == null) {
                    tap = new BoschTapDataProducer(tapDataType, bridge);
                }
                return tap;
            }
        }
        public ILowAndHighGDataProducer LowAndHighG {
            get {
                if (lowHighG == null) {
                    lowHighG = new BoschLowHighDDataProducer(lowHighDataType, bridge);
                }
                return lowHighG;
            }
        }
        public abstract IFlatDataProducer Flat { get; }
        public abstract IMotionDataProducer Motion { get; }

        protected abstract int DataScaleIndex { get; }
        protected abstract float DataScale { get; }
        protected abstract byte MaxOrientHys { get; }
        protected abstract byte[] InitialLowHighGConfig { get; }
        protected abstract float LowHighGDurationStep { get; }

        public float Range => RANGES[DataScaleIndex];
        public abstract float Odr { get; }

        public AccelerometerBosch(IModuleBoardBridge bridge) : base(bridge) {
            accDataType = new BoschCartesianFloatData(DATA_INTERRUPT, 1);
            packedAccDataType = new BoschCartesianFloatData(PACKED_ACC_DATA, 3);
            orientationDataType = new BoschOrientationDataType();
            flatDataType = new BoschFlatDataType();
            tapDataType = new BoschTapDataType();
            lowHighDataType = new BoschLowHighGDataType();
            motionDataType = new BoschMotionDataType();
        }

        public abstract void Configure(float odr = 100, float range = 2f);

        public void Start() {
            bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, POWER_MODE, 0x1 });
        }

        public void Stop() {
            bridge.sendCommand(new byte[] { (byte)ACCELEROMETER, POWER_MODE, 0x0 });
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            collection.Add(accDataType);
            collection.Add(packedAccDataType);
            collection.Add(orientationDataType);
            collection.Add(flatDataType);
            collection.Add(tapDataType);
            collection.Add(lowHighDataType);
            collection.Add(motionDataType);
        }

        public abstract Task PullConfigAsync();
    }
}
