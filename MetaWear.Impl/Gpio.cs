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

            public void Read(byte pullup = 255, byte pulldown = 255, ushort delay = 0, byte virtualPin = 255) {
                var info = bridge.lookupModuleInfo(GPIO);

                if (info.revision >= REVISION_ENHANCED_ANALOG) {
                    analogDataType.read(bridge, new byte[] { pullup, pulldown, (byte)(delay / 4), virtualPin });
                } else {
                    analogDataType.read(bridge);
                }
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
            [DataMember] private readonly byte mask, pin;
            [DataMember] private DataTypeBase adc, absRef, digital, monitor;
            
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
        }

        private class GpioVirtualPin : IVirtualPin {
            private IModuleBoardBridge bridge;
            private readonly byte pin;
            private DataProducer adc, absRef;

            public IDataProducer Adc {
                get {
                    if (adc == null) {
                        adc = new DataProducer(new IntegralDataType(GPIO, Util.setRead(READ_AI_ADC), pin, new DataAttributes(new byte[] { 2 }, 1, 0, false)), bridge);
                    }
                    return adc;
                }
            }

            public IDataProducer AbsoluteReference {
                get {
                    if (absRef == null) {
                        absRef = new DataProducer(new MilliUnitsFloatDataType(GPIO, Util.setRead(READ_AI_ABS_REF), pin, new DataAttributes(new byte[] { 2 }, 1, 0, false)), bridge);
                    }
                    return absRef;
                }
            }

            internal GpioVirtualPin(byte pin, IModuleBoardBridge bridge) {
                this.pin = pin;
                this.bridge = bridge;
            }
        }

        [DataMember] private List<IPin> pins;
        private Dictionary<byte, IVirtualPin> virtualPins = new Dictionary<byte, IVirtualPin>();

        public List<IPin> Pins {
            get {
                return pins;
            }
        }

        public Gpio(IModuleBoardBridge bridge) : base(bridge) {
            var info = bridge.lookupModuleInfo(GPIO);

            pins = new List<IPin>();
            byte pin = 0;
            foreach (byte it in info.extra) {
                pins.Add(new GpioPin(it, pin, bridge));
                pin++;
            }
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            foreach (GpioPin it in pins) {
                it.restoreTransientVars(bridge);
            }
        }

        public IVirtualPin CreateVirtualPin(byte pin) {
            if (virtualPins.TryGetValue(pin, out var virtualPin)) {
                return virtualPin;
            }

            var newPin = new GpioVirtualPin(pin, bridge);
            virtualPins.Add(pin, newPin);

            return newPin;
        }
    }
}
