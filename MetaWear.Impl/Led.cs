using MbientLab.MetaWear.Peripheral;
using MbientLab.MetaWear.Peripheral.Led;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Led : ModuleImplBase, ILed {
        private const byte PLAY = 0x1, STOP = 0x2, CONFIG = 0x3;
        private const byte REVISION_LED_DELAYED = 1;

        private class PatternEditor {
            IModuleBoardBridge bridge;
            byte[] command = new byte[17];

            public PatternEditor(IModuleBoardBridge bridge, Color color) {
                this.bridge = bridge;

                command[0] = (byte) Module.LED;
                command[1] = CONFIG;
                command[2] = (byte) color;
                command[3] = 0x2;

                Count(0xff);
            }

            public PatternEditor HighIntensity(byte intensity) {
                command[4] = intensity;
                return this;
            }

            public PatternEditor LowIntensity(byte intensity) {
                command[5] = intensity;
                return this;
            }

            public PatternEditor RiseTime(ushort time) {
                command[7] = (byte)((time >> 8) & 0xff);
                command[6] = (byte)(time & 0xff);
                return this;
            }

            public PatternEditor HighTime(ushort time) {
                command[9] = (byte)((time >> 8) & 0xff);
                command[8] = (byte)(time & 0xff);
                return this;
            }

            public PatternEditor FallTime(ushort time) {
                command[11] = (byte)((time >> 8) & 0xff);
                command[10] = (byte)(time & 0xff);
                return this;
            }

            public PatternEditor Duration(ushort duration) {
                command[13] = (byte)((duration >> 8) & 0xff);
                command[12] = (byte)(duration & 0xff);
                return this;
            }

            public PatternEditor Delay(ushort delay) {
                if (bridge.lookupModuleInfo(Module.LED).revision >= REVISION_LED_DELAYED) {
                    command[15] = (byte)((delay >> 8) & 0xff);
                    command[14] = (byte)(delay & 0xff);
                }
                else {
                    command[15] = 0;
                    command[14] = 0;
                }
                return this;
            }

            public PatternEditor Count(byte count) {
                command[16] = count;
                return this;
            }

            public void Commit() {
                bridge.sendCommand(command);
            }
        }

        public Led(IModuleBoardBridge bridge) : base(bridge) { }

        public void AutoPlay() {
            bridge.sendCommand(new byte[] { (byte) Module.LED, PLAY, 2 });
        }

        public void Play() {
            bridge.sendCommand(new byte[] { (byte) Module.LED, PLAY, 1 });
        }

        public void Pause() {
            bridge.sendCommand(new byte[] { (byte) Module.LED, PLAY, 0 });
        }

        public void Stop(bool clear) {
            bridge.sendCommand(new byte[] { (byte) Module.LED, STOP, (byte) (clear ? 1 : 0) });
        }

        public void EditPattern(Color color, byte high = 0, byte low = 0,
                ushort riseTime = 0, ushort highTime = 0, ushort fallTime = 0,
                ushort duration = 0, ushort delay = 0, byte count = 0xff) {
            byte[] command = new byte[17] {
                (byte)Module.LED, CONFIG, (byte)color, 0x2,
                high, low,
                (byte)(riseTime & 0xff), (byte)((riseTime >> 8) & 0xff),
                (byte)(highTime & 0xff), (byte)((highTime >> 8) & 0xff),
                (byte)(fallTime & 0xff), (byte)((fallTime >> 8) & 0xff),
                (byte)(duration & 0xff), (byte)((duration >> 8) & 0xff),
                0, 0,
                count
            };
            if (bridge.lookupModuleInfo(Module.LED).revision >= REVISION_LED_DELAYED) {
                command[15] = (byte)((delay >> 8) & 0xff);
                command[14] = (byte)(delay & 0xff);
            }
            bridge.sendCommand(command);
        }

        public void EditPattern(Color color, Pattern preset, ushort delay = 0, byte count = 0xff) {
            switch (preset) {
                case Pattern.Blink:
                    EditPattern(color, high: 31, highTime: 50, duration: 500, delay: delay, count: count);
                    break;
                case Pattern.Pulse:
                    EditPattern(color, high: 31, riseTime: 725, highTime: 500, fallTime: 725, duration: 2000, delay: delay, count: count);
                    break;
                case Pattern.Solid:
                    EditPattern(color, high: 31, low: 31, highTime: 500, duration: 1000, delay: delay, count: count);
                    break;
            }
        }
    }
}
