using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Event : ModuleImplBase, IModule {
        private const byte ENTRY = 2, CMD_PARAMETERS = 3, REMOVE = 4, REMOVE_ALL = 5;

        internal Tuple<Byte, Byte, Byte> feedbackParams = null;
        private TimedTask<byte> createEventTask;
        private Queue<byte[]> recordedCommands;

        internal DataTypeBase ActiveDataType { private set; get; }

        public Event(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            createEventTask = new TimedTask<byte>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)EVENT, ENTRY), response => createEventTask.SetResult(response[2]));
        }

        public override void tearDown() {
            bridge.sendCommand(new byte[] { (byte) EVENT, REMOVE_ALL });
        }

        internal void remove(byte id) {
            bridge.sendCommand(new byte[] { (byte) EVENT, REMOVE, id });
        }

        internal async Task<Queue<byte>> queueEvents(Queue<Tuple<DataTypeBase, Action>> eventCodeBlocks) {
            var successfulEvents = new Queue<byte>();

            try {
                while (eventCodeBlocks.Count != 0) {
                    ActiveDataType = eventCodeBlocks.Peek().Item1;

                    recordedCommands = new Queue<byte[]>();
                    eventCodeBlocks.Peek().Item2();
                    ActiveDataType = null;

                    while (recordedCommands.Count != 0) {
                        var id = await createEventTask.Execute("Programming command timed out after {0}ms", bridge.TimeForResponse * 2, () => {
                            bridge.sendCommand(recordedCommands.Dequeue());
                            bridge.sendCommand(recordedCommands.Dequeue());
                        });

                        successfulEvents.Enqueue(id);
                    }

                    eventCodeBlocks.Dequeue();
                }
            } catch (TimeoutException e) {
                foreach (byte id in successfulEvents) {
                    remove(id);
                }
                throw e;
            }

            return successfulEvents;
        }

        internal void convertToEventCommand(byte[] command) {
            byte[] commandEntry = new byte[] {(byte) EVENT, ENTRY,
                        ActiveDataType.eventConfig[0], ActiveDataType.eventConfig[1], ActiveDataType.eventConfig[2],
                        command[0], command[1], (byte) (command.Length - 2)};

            if (feedbackParams != null) {
                byte[] tempEntry = new byte[commandEntry.Length + 2];
                Array.Copy(commandEntry, 0, tempEntry, 0, commandEntry.Length);

                tempEntry[commandEntry.Length] = (byte)(0x01 | ((feedbackParams.Item1 << 1) & 0xff) | ((feedbackParams.Item2 << 4) & 0xff));
                tempEntry[commandEntry.Length + 1] = feedbackParams.Item3;
                commandEntry = tempEntry;
            }
            recordedCommands.Enqueue(commandEntry);

            byte[] eventParameters = new byte[command.Length];
            Array.Copy(command, 2, eventParameters, 2, command.Length - 2);
            eventParameters[0] = (byte) EVENT;
            eventParameters[1] = CMD_PARAMETERS;
            recordedCommands.Enqueue(eventParameters);
        }
    }
}
