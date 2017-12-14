using MbientLab.MetaWear.Builder;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MbientLab.MetaWear.Impl {
    interface IModuleBoardBridge {
        Task sendCommand(byte[] command);
        Task sendCommand(Module module, byte register, byte[] bytes);
        Task sendCommand(Module module, byte register, byte id, byte[] bytes);
        Task sendCommand(byte[] command, byte dest, IDataToken input);
        Task sendCommand(byte dest, IDataToken input, Module module, byte register, byte id, params byte[] parameters);

        ModuleInfo lookupModuleInfo(Module module);
        Task<IRoute> queueRouteBuilder(Action<IRouteComponent> builder, DataTypeBase source);
        Task<IObserver> queueObserverAsync(Action commands, DataTypeBase source);

        void registerProducerName(string name, DataTypeBase source);
        void removeProducerName(string name);
        void removeRoute(uint id);
        void removeObserver(uint id);

        void addRegisterResponseHandler(Tuple<byte, byte> key, Action<byte[]> handler);
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
