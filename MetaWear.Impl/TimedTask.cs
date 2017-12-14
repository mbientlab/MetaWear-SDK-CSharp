using System;
using System.Threading;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    class TimedTask<T> {
        private TaskCompletionSource<T> taskSource = null;
        private CancellationTokenSource cts = null;

        internal TimedTask() { }

        internal async Task<T> Execute(string format, int timeout, Action action) {
            taskSource = new TaskCompletionSource<T>();
            cts = new CancellationTokenSource();

            action();
            if (timeout != 0) {
                // use task timeout pattern from https://stackoverflow.com/a/11191070
                var delay = Task.Delay(timeout, cts.Token);
                if (await Task.WhenAny(taskSource.Task, delay) != taskSource.Task) {
                    if (!delay.IsCanceled) {
                        taskSource.SetException(new TimeoutException(string.Format(format, timeout)));
                    }
                } else {
                    cts.Cancel();
                }
            }
            return await taskSource.Task;
        }

        internal void SetResult(T result) {
            taskSource.TrySetResult(result);
        }

        internal void SetError(Exception e) {
            taskSource.TrySetException(e);
        }
    }
}