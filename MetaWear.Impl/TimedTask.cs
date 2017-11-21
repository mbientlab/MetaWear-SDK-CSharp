using System;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    class TimedTask<T> {
        private TaskCompletionSource<T> taskSource = null;

        internal TimedTask() { }

        internal async Task<T> Execute(string format, int timeout, Action action) {
            taskSource = new TaskCompletionSource<T>();

            action();
            if (timeout != 0) {
                // use task timeout pattern from https://stackoverflow.com/a/11191070
                if (await Task.WhenAny(taskSource.Task, Task.Delay(timeout)) == taskSource.Task) {
                    return await taskSource.Task;
                } else {
                    throw new TimeoutException(string.Format(format, timeout));
                }
            } else {
                return await taskSource.Task;
            }
        }

        internal void SetResult(T result) {
            taskSource.TrySetResult(result);
        }

        internal void SetError(Exception e) {
            taskSource.TrySetException(e);
        }
    }
}