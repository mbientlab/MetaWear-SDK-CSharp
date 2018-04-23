using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Macro : ModuleImplBase, IMacro {
        private const int WRITE_MACRO_DELAY = 2000;
        private const byte ENABLE = 0x1,
                BEGIN = 0x2, ADD_COMMAND = 0x3, END = 0x4,
                EXECUTE = 0x5, NOTIFY_ENABLE = 0x6, NOTIFY = 0x7,
                ERASE_ALL = 0x8,
                ADD_PARTIAL = 0x9;

        internal bool isRecording = false;
        internal Queue<byte[]> commands;
        private TimedTask<byte> beginMacroTask;
        private bool execOnBoot;

        public Macro(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            beginMacroTask = new TimedTask<byte>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)MACRO, BEGIN), response => beginMacroTask.SetResult(response[2]));
        }

        public async Task<byte> EndRecordAsync() {
            await Task.Delay(WRITE_MACRO_DELAY);
            var id = await beginMacroTask.Execute("Did not receive macro id within {0}ms", bridge.TimeForResponse, 
                () => {
                    isRecording = false;
                    bridge.sendCommand(new byte[] { (byte)MACRO, BEGIN, (byte)(execOnBoot ? 1 : 0) });
                });

            foreach (var it in commands) {
                foreach (var cmd in convertToMacroCommand(it)) {
                    bridge.sendCommand(cmd);
                }
            }

            bridge.sendCommand(new byte[] { (byte)MACRO, END });

            await bridge.waitForCommands();

            return id;
        }

        public void EraseAll() {
            bridge.sendCommand(new byte[] { (byte) MACRO, ERASE_ALL });
        }

        public void Execute(byte id) {
            bridge.sendCommand(new byte[] { (byte) MACRO, EXECUTE, id });
        }

        public void StartRecord(bool execOnBoot = true) {
            isRecording = true;
            commands = new Queue<byte[]>();
            this.execOnBoot = execOnBoot;
        }

        private byte[][] convertToMacroCommand(byte[] command) {
            if (command.Length >= MetaWearBoard.COMMAND_LENGTH) {
                byte[][] macroCmds = new byte[2][];

                byte PARTIAL_LENGTH = 2;
                macroCmds[0] = new byte[PARTIAL_LENGTH + 2];
                macroCmds[0][0] = (byte) MACRO;
                macroCmds[0][1] = ADD_PARTIAL;
                Array.Copy(command, 0, macroCmds[0], 2, PARTIAL_LENGTH);

                macroCmds[1] = new byte[command.Length - PARTIAL_LENGTH + 2];
                macroCmds[1][0] = (byte) MACRO;
                macroCmds[1][1] = ADD_COMMAND;
                Array.Copy(command, PARTIAL_LENGTH, macroCmds[1], 2, macroCmds[1].Length - 2);

                return macroCmds;
            } else {
                byte[][] macroCmds = new byte[1][];
                macroCmds[0] = new byte[command.Length + 2];
                macroCmds[0][0] = (byte) MACRO;
                macroCmds[0][1] = ADD_COMMAND;
                Array.Copy(command, 0, macroCmds[0], 2, command.Length);

                return macroCmds;
            }
        }
    }
}
