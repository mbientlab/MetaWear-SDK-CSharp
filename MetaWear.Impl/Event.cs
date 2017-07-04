using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Event : ModuleImplBase, IModule {
        private const byte ENTRY = 2, CMD_PARAMETERS = 3, REMOVE = 4, REMOVE_ALL = 5;

        internal Tuple<Byte, Byte, Byte> feedbackParams = null;
        private Timer eventTimeoutFuture;
        private Queue<byte> successfulEvents;
        private Queue<Tuple<DataTypeBase, Action>> pendingEventCodeBlocks;
        private TaskCompletionSource<Queue<byte>> createEventsTask;
        private Queue<byte[]> recordedCommands;
        private bool isRecording = false;
        private TimerCallback callback;

        public Event(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            callback = e => {
                foreach (byte id in successfulEvents) {
                    remove(id);
                }

                pendingEventCodeBlocks = null;
                recordedCommands = null;
                createEventsTask.SetException(new TimeoutException("Timed out programming commands"));
            };

            bridge.addRegisterResponseHandler(Tuple.Create((byte)EVENT, ENTRY), response => {
                eventTimeoutFuture.Dispose();

                successfulEvents.Enqueue(response[2]);
                if (recordedCommands.Count != 0) {
                    bridge.sendCommand(recordedCommands.Dequeue());
                    bridge.sendCommand(recordedCommands.Dequeue());

                    eventTimeoutFuture = new Timer(callback, null, 250, Timeout.Infinite);
                } else {
                    pendingEventCodeBlocks.Dequeue();
                    recordCommand();
                }
            });
        }

        public override void tearDown() {
            bridge.sendCommand(new byte[] { (byte) EVENT, REMOVE_ALL });
        }

        internal void remove(byte id) {
            bridge.sendCommand(new byte[] { (byte) EVENT, REMOVE, id });
        }

        internal Task<Queue<byte>> queueEvents(Queue<Tuple<DataTypeBase, Action>> eventCodeBlocks) {
            successfulEvents = new Queue<byte>();
            pendingEventCodeBlocks = eventCodeBlocks;
            createEventsTask = new TaskCompletionSource<Queue<byte>>();
            recordCommand();

            return createEventsTask.Task;
        }

        internal byte[] getEventConfig() {
            return isRecording ? pendingEventCodeBlocks.Peek().Item1.eventConfig : null;
        }

        internal void convertToEventCommand(byte[] command) {
            byte[] eventConfig = getEventConfig();
            byte[] commandEntry = new byte[] {(byte) EVENT, ENTRY,
                        eventConfig[0], eventConfig[1], eventConfig[2],
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

        private void recordCommand() {
            if (pendingEventCodeBlocks.Count != 0) {
                isRecording = true;
                recordedCommands = new Queue<byte[]>();
                pendingEventCodeBlocks.Peek().Item2();
                isRecording = false;

                eventTimeoutFuture = new Timer(callback, null, 250, Timeout.Infinite);
                bridge.sendCommand(recordedCommands.Dequeue());
                bridge.sendCommand(recordedCommands.Dequeue());
                
            } else {
                createEventsTask.SetResult(successfulEvents);
            }
        }
    }
}
