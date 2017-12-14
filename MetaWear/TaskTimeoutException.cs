using System;

namespace MbientLab.MetaWear {
    /// <summary>
    /// Extension of the <see cref="TimeoutException"/> class that contains a partial result of the task
    /// </summary>
    public class TaskTimeoutException : TimeoutException {
        /// <summary>
        /// Partial result of the task
        /// </summary>
        public object PartialResult { get; private set; }
        
        /// <summary>
        /// Creates an exception with the given cause and partial result
        /// </summary>
        /// <param name="innerException">Exception that caused this one</param>
        /// <param name="partial">Partial result of the task</param>
        public TaskTimeoutException(Exception innerException, object partial) : base("dummy message", innerException) {
            PartialResult = partial;
        }
    }
}
