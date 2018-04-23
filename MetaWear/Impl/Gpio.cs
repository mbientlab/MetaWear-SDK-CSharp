using System;
using System.Collections.Generic;

using static MbientLab.MetaWear.Impl.Module;
using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Builder;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Peripheral.Gpio;

namespace MbientLab.MetaWear.Impl {
    [KnownType(typeof(GpioPin))]
    [DataContract]
    class Gpio : ModuleImplBase, IGpio {
        internal static string createIdentifier(DataTypeBase dataType) {
            switch (Util.clearRead(dataType.eventConfig[1])) {
                case READ_AI_ABS_REF:
                    return string.Format("abs-ref[{0}]", dataType.eventConfig[2]);
                case READ_AI_ADC:
                    return string.Format("adc[{0}]", dataType.eventConfig[2]);
                case READ_DI:
                    return string.Format("digital[{0}]", dataType.eventConfig[2]);
                case PIN_CHANGE_NOTIFY:
                    return string.Format("pin-monitor[{0}]", dataType.eventConfig[2]);
                default:
                    return null;
            }
        }

        private const byte REVISION_ENHANCED_ANALOG = 2;
        private const byte SET_DO = 1, CLEAR_DO = 2,
                PULL_UP_DI = 3, PULL_DOWN_DI = 4, NO_PULL_DI = 5,
                READ_AI_ABS_REF = 6, READ_AI_ADC = 7, READ_DI = 8,
                PIN_CHANGE = 9, PIN_CHANGE_NOTIFY = 10,
                PIN_CHANGE_NOTIFY_ENABLE = 11;

        private class GpioAnalogDataProducer : IAnalogDataProducer {
            private DataTypeBase analogDataType;
            private IModuleBoardBridge bridge;

            internal GpioAnalogDataProducer(DataTypeBase analogDataType, byte pin, IModuleBoardBridge bridge) {
                this.bridge = bridge;
                this.analogDataType = analogDataType;
            }

            public Task<IRoute> AddRouteAsync(Action<IRouteComponent> builder) {
                return bridge.queueRouteBuilder(builder, analogDataType);
            }

            public void Read(byte pullup = 255, byte pulldown = 255, ushort delay = 0) {
                var info = bridge.lookupModuleInfo(GPIO);

                if (info.revision >= REVISION_ENHANCED_ANALOG) {
                    analogDataType.read(bridge, new byte[] { pullup, pulldown, (byte)(delay / 4), 255 });
                } else {
                    analogDataType.read(bridge);
                }
            }

            void IForcedDataProducer.Read() {
                Read();
            }
        }

        private class GpioVirtualAnalogDataProducer : IAnalogDataProducer {
            private DataTypeBase source, analogDataType;
            private IModuleBoardBridge bridge;

            internal GpioVirtualAnalogDataProducer(DataTypeBase source, byte pin, IModuleBoardBridge bridge) {
                this.source = source;
                this.bridge = bridge;
                analogDataType = source.copy(source.input, GPIO, source.eventConfig[1], pin, source.attributes); ;
            }

            public Task<IRoute> AddRouteAsync(Action<IRouteComponent> builder) {
                return bridge.queueRouteBuilder(builder, analogDataType);
            }

            public void Read(byte pullup = 255, byte pulldown = 255, ushort delay = 0) {
                source.read(bridge, analogDataType.eventConfig[1], new byte[] { pullup, pulldown, (byte)(delay / 4), analogDataType.eventConfig[2] });
            }

            void IForcedDataProducer.Read() {
                Read();
            }
        }

        private class GpioMonitorDataProducer : AsyncDataProducer {
            internal GpioMonitorDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge) : 
                base(PIN_CHANGE_NOTIFY_ENABLE, dataTypeBase, bridge) { }

            public override void Start() {
                bridge.sendCommand(new byte[] { (byte)GPIO, PIN_CHANGE_NOTIFY_ENABLE, dataTypeBase.eventConfig[2], 0x01 });
            }

            public override void Stop() {
                bridge.sendCommand(new byte[] { (byte)GPIO, PIN_CHANGE_NOTIFY_ENABLE, dataTypeBase.eventConfig[2], 0x00 });
            }
        }

        [KnownType(typeof(MilliUnitsFloatDataType))]
        [KnownType(typeof(IntegralDataType))]
        [DataContract]
        private class GpioPin : SerializableType, IPin {
            [DataMember] internal readonly byte mask, pin;
            [DataMember] internal DataTypeBase adc, absRef, digital, monitor;
            
            private IAnalogDataProducer adcProducer = null, absRefProducer = null;
            private IForcedDataProducer digitalProducer = null;
            private IAsyncDataProducer monitorProducer = null;

            public IAnalogDataProducer Adc {
                get {
                    if (adc != null && adcProducer == null) {
                        adcProducer = new GpioAnalogDataProducer(adc, pin, bridge);
                    }
                    return adcProducer;
                }
            }

            public IAnalogDataProducer AbsoluteReference {
                get {
                    if (absRef != null && adcProducer == null) {
                        absRefProducer = new GpioAnalogDataProducer(absRef, pin, bridge);
                    }
                    return absRefProducer;
                }
            }

            public IForcedDataProducer Digital {
                get {
                    if (digital != null && digitalProducer == null) {
                        digitalProducer = new ForcedDataProducer(digital, bridge);
                    }
                    return digitalProducer;
                }
            }

            public IAsyncDataProducer Monitor {
                get {
                    if (monitor != null && monitorProducer == null) {
                        monitorProducer = new GpioMonitorDataProducer(monitor, bridge);
                    }
                    return monitorProducer;
                }
            }

            internal GpioPin(byte mask, byte pin, IModuleBoardBridge bridge) : base(bridge) {
                this.mask = mask;
                this.pin = pin;

                if ((mask & 0x2) == 0x2) {
                    adc = new IntegralDataType(GPIO, Util.setRead(READ_AI_ADC), pin, new DataAttributes(new byte[] { 2 }, 1, 0, false));
                    absRef = new MilliUnitsFloatDataType(GPIO, Util.setRead(READ_AI_ABS_REF), pin, new DataAttributes(new byte[] { 2 }, 1, 0, false));
                }
                if ((mask & 0x1) == 0x1) {
                    digital = new IntegralDataType(GPIO, Util.setRead(READ_DI), pin, new DataAttributes(new byte[] { 1 }, 1, 0, false));
                    monitor = new IntegralDataType(GPIO, PIN_CHANGE_NOTIFY, pin, new DataAttributes(new byte[] { 1 }, 1, 0, false));
                }
            }

            public void ClearOutput() {
                bridge.sendCommand(new byte[] { (byte) GPIO, CLEAR_DO, pin });
            }

            public void SetChangeType(PinChangeType type) {
                bridge.sendCommand(new byte[] { (byte) GPIO, PIN_CHANGE, pin, (byte)(type + 1) });
            }

            public void SetOutput() {
                bridge.sendCommand(new byte[] { (byte) GPIO, SET_DO, pin });
            }

            public void SetPullMode(PullMode mode) {
                switch (mode) {
                    case PullMode.Up:
                        bridge.sendCommand(new byte[] { (byte) GPIO, PULL_UP_DI, pin });
                        break;
                    case PullMode.Down:
                        bridge.sendCommand(new byte[] { (byte) GPIO, PULL_DOWN_DI, pin });
                        break;
                    case PullMode.None:
                        bridge.sendCommand(new byte[] { (byte) GPIO, NO_PULL_DI, pin });
                        break;
                }
            }

            public IVirtualPin CreateVirtualPin(byte pin) {
                var gpio = bridge.GetModule<IGpio>() as Gpio;
                if (gpio.virtualPins.TryGetValue(pin, out var virtualPin)) {
                    return virtualPin;
                }

                var newPin = new GpioVirtualPin(adc, absRef, pin, bridge);
                gpio.virtualPins.Add(pin, newPin);

                return newPin;
            }
        }

        private class GpioVirtualPin : IVirtualPin {
            private byte pin;
            private IModuleBoardBridge bridge;
            private IAnalogDataProducer adc, absRef;
            internal DataTypeBase adcType, absRefType;

            public IAnalogDataProducer Adc {
                get {
                    if (adc == null) {
                        adc = new GpioVirtualAnalogDataProducer(adcType, pin, bridge);
                    }
                    return adc;
                }
            }

            public IAnalogDataProducer AbsoluteReference {
                get {
                    if (absRef == null) {
                        absRef = new GpioVirtualAnalogDataProducer(absRefType, pin, bridge);
                    }
                    return absRef;
                }
            }

            internal GpioVirtualPin(DataTypeBase adc, DataTypeBase absRef, byte pin, IModuleBoardBridge bridge) {
                adcType = adc;
                absRefType = absRef;
                this.pin = pin;
                this.bridge = bridge;
            }
        }

        [DataMember] private List<IPin> pins;
        internal Dictionary<byte, IVirtualPin> virtualPins;

        public List<IPin> Pins => pins;

        public Gpio(IModuleBoardBridge bridge) : base(bridge) {
            var info = bridge.lookupModuleInfo(GPIO);
            
            pins = new List<IPin>();
            byte pin = 0;
            foreach (byte it in info.extra) {
                pins.Add(new GpioPin(it, pin, bridge));
                pin++;
            }
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            foreach(var p in pins) {
                var casted = p as GpioPin;
                if (casted.adc != null) {
                    collection.Add(casted.adc);
                }
                if (casted.absRef != null) {
                    collection.Add(casted.absRef);
                }
                if (casted.digital != null) {
                    collection.Add(casted.digital);
                }
                if (casted.monitor != null) {
                    collection.Add(casted.monitor);
                }
            }

            foreach (var p in virtualPins.Values) {
                var vPin = p as GpioVirtualPin;
                if (vPin.adcType != null) {
                    collection.Add((p as GpioVirtualPin).adcType);
                }
                if (vPin.absRefType != null) {
                    collection.Add((p as GpioVirtualPin).absRefType);
                }
            }
        }

        protected override void init() {
            virtualPins = new Dictionary<byte, IVirtualPin>();
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            base.restoreTransientVars(bridge);

            foreach (GpioPin it in pins) {
                it.restoreTransientVars(bridge);
            }
        }
    }
}
