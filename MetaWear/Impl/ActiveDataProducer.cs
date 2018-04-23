using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    class ActiveDataProducer<T> : DataProducer, IActiveDataProducer<T> {
        private readonly TimedTask<T> readTask;

        internal ActiveDataProducer(DataTypeBase dataTypeBase, IModuleBoardBridge bridge)  : base(dataTypeBase, bridge) {
            readTask = new TimedTask<T>();
        }

        public async Task<T> ReadAsync() {
            var cmd = dataTypeBase.createReadStateCmd();
            return await readTask.Execute("Did not receive a response for command " + Util.ArrayToHexString(cmd) + " within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(cmd));
        }

        internal void SetReadResult(T result) {
            readTask.SetResult(result);
        }
    }
}
