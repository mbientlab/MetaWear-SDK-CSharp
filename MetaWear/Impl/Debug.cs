using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Debug : ModuleImplBase, IDebug {
        private const byte POWER_SAVE_REVISION = 0x1;

        internal TaskCompletionSource<bool> dcTaskSource;
        private TimedTask<byte[]> readTmpValueTask;

        private const byte TMP_VALUE = 0x4;

        public Debug(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            dcTaskSource = null;

            readTmpValueTask = new TimedTask<byte[]>();
            bridge.addRegisterResponseHandler(Tuple.Create((byte)DEBUG, Util.setRead(TMP_VALUE)), response => readTmpValueTask.SetResult(response));
        }

        public override void disconnected() {
            if (dcTaskSource != null) {
                dcTaskSource.TrySetResult(true);
                dcTaskSource = null;
            }
        }

        private Task SetupDisconnect(byte[] cmd) {
            dcTaskSource = new TaskCompletionSource<bool>();

            bridge.sendCommand(cmd);
            if (bridge.GetModule<Event>()?.ActiveDataType != null) {
                dcTaskSource.SetCanceled();
            }

            return dcTaskSource.Task;
        }

        public Task DisconnectAsync() {
            return SetupDisconnect(new byte[] { (byte)DEBUG, 0x6 });
        }

        public Task JumpToBootloaderAsync() {
            return SetupDisconnect(new byte[] { (byte)DEBUG, 0x2 });
        }

        public void ResetAfterGc() {
            bridge.sendCommand(new byte[] { (byte) DEBUG, 0x5 });
        }

        public Task ResetAsync() {
            return SetupDisconnect(new byte[] { (byte)DEBUG, 0x1 });
        }

        public bool EnablePowerSave() {
            if (bridge.lookupModuleInfo(DEBUG).revision >= POWER_SAVE_REVISION) {
                bridge.sendCommand(new byte[] { (byte)DEBUG, 0x07 });
                return true;
            }
            return false;
        }

        public void WriteTmpValue(int value) {
            bridge.sendCommand(DEBUG, TMP_VALUE, Util.intToBytesLe(value));
        }

        public async Task<int> ReadTmpValueAsync() {
            var result = await readTmpValueTask.Execute("Did not received response from tmp register within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)DEBUG, Util.setRead(TMP_VALUE) }));
            return Util.bytesLeToInt(result, 2);
        }
    }
}
