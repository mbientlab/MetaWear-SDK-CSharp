using MbientLab.MetaWear.Builder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    interface IModuleBoardBridge {
        Action<string, Exception> OnError { get; }
        Task waitForCommands();

        void sendCommand(byte[] command);
        void sendCommand(Module module, byte register, byte[] bytes);
        void sendCommand(Module module, byte register, byte id, byte[] bytes);
        void sendCommand(byte[] command, byte dest, IDataToken input);
        void sendCommand(byte dest, IDataToken input, Module module, byte register, byte id, params byte[] parameters);

        ModuleInfo lookupModuleInfo(Module module);
        Task<IRoute> queueRouteBuilder(Action<IRouteComponent> builder, DataTypeBase source);
        Task<IObserver> queueObserverAsync(Action commands, DataTypeBase source);

        void registerProducerName(string name, DataTypeBase source);
        void removeProducerName(string name);
        void removeRoute(uint id);
        void removeObserver(uint id);

        void addRegisterResponseHandler(Tuple<byte, byte> key, Action<byte[]> handler);
        void removeRegisterResponseHandler(Tuple<byte, byte> key);
        void addDataIdHeader(Tuple<byte, byte> key);
        void addDataHandler(Tuple<byte, byte, byte> key, Action<byte[]> handler);
        void removeDataHandler(Tuple<byte, byte, byte> key, Action<byte[]> handler);
        int numDataHandlers(Tuple<byte, byte, byte> key);

        T GetModule<T>() where T : class, IModule;

        Version getFirmware();
        int TimeForResponse { get; }

        ICollection<DataTypeBase> aggregateDataSources();
    }
}
