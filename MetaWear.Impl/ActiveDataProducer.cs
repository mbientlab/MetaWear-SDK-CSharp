using System;
using System.Threading.Tasks;
using System.Threading;

namespace MbientLab.MetaWear.Impl {
    class ActiveDataProducer<T> : DataProducer, IActiveDataProducer<T> {
        private Timer readTimeoutFuture;
        private TaskCompletionSource<T> readTask = null;

        internal ActiveDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge)  : base(dataTypeBase, bridge) {
        }

        public Task<T> ReadAsync() {
            readTask = new TaskCompletionSource<T>();
            readTimeoutFuture = new Timer(e => {
                if (readTask != null) {
                    readTask.SetException(new TimeoutException("Reading current state timed out"));
                    readTask = null;
                }
            }, null, 250, Timeout.Infinite);
            bridge.sendCommand(dataTypeBase.createReadStateCmd());
            return readTask.Task;
        }

        internal void SetReadResult(T result) {
            readTimeoutFuture.Dispose();

            if (readTask != null) {
                readTask.SetResult(result);
                readTask = null;
            }
        }
    }
}
