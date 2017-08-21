using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Core;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class StreamedDataConsumer : DeviceDataConsumer {
        protected Action<byte[]> dataResponseHandler = null;

        public StreamedDataConsumer(DataTypeBase source, Action<IData> subscriber) : base(source, subscriber) { 
        }

        public override void enableStream(IModuleBoardBridge bridge) {
            addDataHandler(bridge);

            if ((source.eventConfig[1] & 0x80) == 0x0) {
                if (source.eventConfig[2] == DataTypeBase.NO_ID) {
                    if (bridge.numDataHandlers(source.eventConfigAsTuple()) == 1) {
                        bridge.sendCommand(new byte[] { source.eventConfig[0], source.eventConfig[1], 0x1 });
                    }
                } else {
                    bridge.sendCommand(new byte[] { source.eventConfig[0], source.eventConfig[1], 0x1 });
                    if (bridge.numDataHandlers(source.eventConfigAsTuple()) == 1) {
                        if (source.eventConfig[0] == (byte) Module.DATA_PROCESSOR && source.eventConfig[1] == DataProcessor.NOTIFY) {
                            bridge.sendCommand(new byte[] { source.eventConfig[0], DataProcessor.NOTIFY_ENABLE, source.eventConfig[2], 0x1 });
                        }
                    }
                }
            } else {
                source.markLive();
            }
        }

        public override void disableStream(IModuleBoardBridge bridge) {
            if ((source.eventConfig[1] & 0x80) == 0x0) {
                if (source.eventConfig[2] == DataTypeBase.NO_ID) {
                    if (bridge.numDataHandlers(source.eventConfigAsTuple()) == 1) {
                        bridge.sendCommand(new byte[] { source.eventConfig[0], source.eventConfig[1], 0x0 });
                    }
                } else {
                    if (bridge.numDataHandlers(source.eventConfigAsTuple()) == 1) {
                        if (source.eventConfig[0] == (byte)Module.DATA_PROCESSOR && source.eventConfig[1] == DataProcessor.NOTIFY) {
                            bridge.sendCommand(new byte[] { source.eventConfig[0], DataProcessor.NOTIFY_ENABLE, source.eventConfig[2], 0x0 });
                        }
                    }
                }
            } else {
                if (bridge.numDataHandlers(source.eventConfigAsTuple()) == 1) {
                    source.markSilent();
                }
            }

            bridge.removeDataHandler(source.eventConfigAsTuple(), dataResponseHandler);
        }

        public override void addDataHandler(IModuleBoardBridge bridge) {
            if (source.eventConfig[2] != DataTypeBase.NO_ID) {
                bridge.addDataIdHeader(Tuple.Create(source.eventConfig[0], source.eventConfig[1]));
            }

            if (dataResponseHandler == null) {
                if (source.attributes.copies > 1) {
                    byte dataUnitLength = source.attributes.unitLength();
                    dataResponseHandler = response => {
                        DateTime now = DateTime.Now;
                        Tuple<DataTypeBase, EditorImplBase> accounter = findParent(bridge.GetModule<IDataProcessor>() as DataProcessor, source, DataProcessor.TYPE_ACCOUNTER);
                        for (int i = 0, j = source.eventConfig[2] == DataTypeBase.NO_ID ? 2 : 3; i < source.attributes.copies && j < response.Length; i++, j += dataUnitLength) {
                            Tuple<DateTime, int> account = fillTimestamp(bridge, accounter, response, j);
                            byte[] dataRaw = new byte[dataUnitLength - (account.Item2 - j)];
                            Array.Copy(response, account.Item2, dataRaw, 0, dataRaw.Length);
                            call(source.createData(false, bridge, dataRaw, accounter == null ? now : account.Item1));
                        }
                    };
                } else {
                    dataResponseHandler = response => {
                        byte[] dataRaw;

                        if (source.eventConfig[2] == DataTypeBase.NO_ID) {
                            dataRaw = new byte[response.Length - 2];
                            Array.Copy(response, 2, dataRaw, 0, dataRaw.Length);
                        } else {
                            dataRaw = new byte[response.Length - 3];
                            Array.Copy(response, 3, dataRaw, 0, dataRaw.Length);
                        }

                        Tuple<DateTime, int> account = fillTimestamp(bridge, source, dataRaw);
                        if (account.Item2 > 0) {
                            byte[] copy = new byte[dataRaw.Length - account.Item2];
                            Array.Copy(dataRaw, account.Item2, copy, 0, copy.Length);
                            dataRaw = copy;
                        }

                        Tuple<DataTypeBase, EditorImplBase> packer = findParent(bridge.GetModule<IDataProcessor>() as DataProcessor, source, DataProcessor.TYPE_PACKER);
                        if (packer != null) {
                            byte dataUnitLength = packer.Item2.source.attributes.unitLength();
                            byte[] unpacked = new byte[dataUnitLength];
                            for (int i = 0, j = 3 + account.Item2; i < packer.Item2.source.attributes.copies && j < response.Length; i++, j += dataUnitLength) {
                                Array.Copy(response, j, unpacked, 0, unpacked.Length);
                                call(source.createData(false, bridge, unpacked, account.Item1));
                            }
                        } else {
                            call(source.createData(false, bridge, dataRaw, account.Item1));
                        }
                    };
                }
            }

            bridge.addDataHandler(source.eventConfigAsTuple(), dataResponseHandler);
        }

        private static Tuple<DataTypeBase, EditorImplBase> findParent(DataProcessor dataprocessor, DataTypeBase child, byte type) {
            if (child.eventConfig[0] == (byte)DATA_PROCESSOR && child.eventConfig[1] == DataProcessor.NOTIFY) {
                var processor = dataprocessor.lookupProcessor(child.eventConfig[2]);
                if (processor.Item2.config[0] == type) {
                    return processor;
                }

                return findParent(dataprocessor, child.input, type);
            }
            return null;
        }

        private static Tuple<DateTime, int> fillTimestamp(IModuleBoardBridge bridge, DataTypeBase source, byte[] response) {
            if (source.eventConfig[0] == (byte) DATA_PROCESSOR && source.eventConfig[1] == DataProcessor.NOTIFY) {
                var dataprocessor = bridge.GetModule<IDataProcessor>() as DataProcessor;
                return fillTimestamp(bridge, dataprocessor.lookupProcessor(source.eventConfig[2]), response, 0);
            }
            return Tuple.Create(DateTime.Now, 0);
        }

        private static Tuple<DateTime, int> fillTimestamp(IModuleBoardBridge bridge, Tuple<DataTypeBase, EditorImplBase> accounter, byte[] response, int offset) {
            if (accounter != null) {
                byte[] config = accounter.Item2.config;
                if (config[0] == DataProcessor.TYPE_ACCOUNTER) {
                    var logging = bridge.GetModule<ILogging>() as Logging;
                    int size = ((config[1] & 0x30) >> 4) + 1;
                    uint tick = BitConverter.ToUInt32(response, offset);

                    return Tuple.Create(logging.computeTimestamp(0xff, tick), size + offset);
                }
            }
            return Tuple.Create(DateTime.Now, offset);
        }
    }
}
