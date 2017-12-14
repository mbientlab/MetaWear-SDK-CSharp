using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.Module;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Debug : ModuleImplBase, IDebug {
        internal TaskCompletionSource<bool> dcTaskSource;

        public Debug(IModuleBoardBridge bridge) : base(bridge) {
        }

        protected override void init() {
            dcTaskSource = null;
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
    }
}
