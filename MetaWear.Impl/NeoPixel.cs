using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.NeoPixel;
using static MbientLab.MetaWear.Impl.Module;

using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class NeoPixel : ModuleImplBase, INeoPixel {
        private const byte INITIALIZE = 1,
            HOLD = 2,
            CLEAR = 3, SET_COLOR = 4,
            ROTATE = 5,
            FREE = 6;

        [DataContract]
        private class Strand : SerializableType, IStrand {
            private int nLeds;
            private byte id;

            public Strand(byte id, int nLeds, IModuleBoardBridge bridge) : base(bridge) {
                this.nLeds = nLeds;
                this.id = id;
            }

            public int NLeds => nLeds;

            public void Clear(byte start, byte end) {
                bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, CLEAR, id, start, end });
            }

            public void Free() {
                bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, FREE, id });
                (bridge.GetModule<INeoPixel>() as NeoPixel).strands[id] = null;
            }

            public void Hold() {
                bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, HOLD, id, 0x1 });
            }

            public void Release() {
                bridge.sendCommand(new byte[] { (byte)NEO_PIXEL, HOLD, id, 0x0 });
            }

            public void Rotate(RotationDirection direction, ushort period, byte repetitions = 255) {
                bridge.sendCommand(new byte[] { (byte)NEO_PIXEL, ROTATE, id, (byte)direction, repetitions,
                        (byte)(period & 0xff), (byte)(period >> 8 & 0xff)});
            }

            public void SetRgb(byte index, byte red, byte green, byte blue) {
                bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, SET_COLOR, id, index, red, green, blue });
            }

            public void StopRotation() {
                bridge.sendCommand(new byte[] { (byte)NEO_PIXEL, ROTATE, id, 0x0, 0x0, 0x0, 0x0 });
            }
        }

        [DataMember] private Strand[] strands = new Strand[3];

        public NeoPixel(IModuleBoardBridge bridge) : base(bridge) {
        }

        public IStrand InitializeStrand(byte id, ColorOrdering ordering, StrandSpeed speed, byte gpioPin, byte nLeds) {
            strands[id] = new Strand(id, nLeds, bridge);
            bridge.sendCommand(new byte[] { (byte) NEO_PIXEL, INITIALIZE, id, (byte)((byte) speed << 2 | (byte) ordering), gpioPin, nLeds });
            return strands[id];
        }

        public IStrand LookupStrand(byte id) {
            return strands[id];
        }
    }
}
