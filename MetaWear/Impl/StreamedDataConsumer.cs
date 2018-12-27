using static MbientLab.MetaWear.Impl.Module;

using System;
using System.Runtime.Serialization;
using MbientLab.MetaWear.Core;
using static MbientLab.MetaWear.Impl.DataProcessorConfig;
using MbientLab.MetaWear.Builder;

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
                        if (source.eventConfig[0] == (byte)DATA_PROCESSOR && source.eventConfig[1] == DataProcessor.NOTIFY) {
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
                        if (source.eventConfig[0] == (byte)DATA_PROCESSOR && source.eventConfig[1] == DataProcessor.NOTIFY) {
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
                Func<Type, object> accounterExtra(uint value) {
                    return type => {
                        if (type == typeof(uint)) {
                            return value;
                        }
                        return null;
                    };
                }

                if (source.attributes.copies > 1) {
                    byte dataUnitLength = source.attributes.unitLength();
                    dataResponseHandler = response => {
                        DateTime now = DateTime.Now;
                        var accounter = findParent(bridge.GetModule<IDataProcessor>() as DataProcessor, source, DataProcessor.TYPE_ACCOUNTER);
                        AccountType accountType = accounter != null ? (accounter.Item2.configObj as AccounterConfig).type : AccountType.Time;

                        for (int i = 0, j = source.eventConfig[2] == DataTypeBase.NO_ID ? 2 : 3; i < source.attributes.copies && j < response.Length; i++, j += dataUnitLength) {
                            var account = fillTimestamp(bridge, accounter, response, j);
                            byte[] dataRaw = new byte[dataUnitLength - (account.Item2 - j)];
                            Array.Copy(response, account.Item2, dataRaw, 0, dataRaw.Length);

                            var data = source.createData(false, bridge, dataRaw, accounter == null || accountType == AccountType.Count ? now : account.Item1);
                            if (accountType == AccountType.Count) {
                                data.extraFn = accounterExtra(account.Item3);
                            }
                            call(data);
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

                        AccountType accountType = AccountType.Time;
                        Tuple<DateTime, int, uint> extra;
                        if (source.eventConfig[0] == (byte)DATA_PROCESSOR && source.eventConfig[1] == DataProcessor.NOTIFY) {
                            var dataprocessor = bridge.GetModule<IDataProcessor>() as DataProcessor;
                            var processor = dataprocessor.lookupProcessor(source.eventConfig[2]);
                            extra = fillTimestamp(bridge, processor, dataRaw, 0);

                            if (extra.Item2 > 0) {
                                byte[] copy = new byte[dataRaw.Length - extra.Item2];
                                Array.Copy(dataRaw, extra.Item2, copy, 0, copy.Length);
                                dataRaw = copy;
                                accountType = (processor.Item2.configObj as AccounterConfig).type;
                            }
                        } else {
                            extra = Tuple.Create(DateTime.Now, 0, (uint)0);
                        }
                        
                        var packer = findParent(bridge.GetModule<IDataProcessor>() as DataProcessor, source, DataProcessor.TYPE_PACKER);
                        if (packer != null) {
                            byte dataUnitLength = packer.Item2.source.attributes.unitLength();
                            byte[] unpacked = new byte[dataUnitLength];
                            for (int i = 0, j = 3 + extra.Item2; i < packer.Item2.source.attributes.copies && j < response.Length; i++, j += dataUnitLength) {
                                Array.Copy(response, j, unpacked, 0, unpacked.Length);

                                var data = source.createData(false, bridge, unpacked, extra.Item1);
                                if (accountType == AccountType.Count) {
                                    data.extraFn = accounterExtra(extra.Item3);
                                }
                                call(data);
                            }
                        } else {
                            var data = source.createData(false, bridge, dataRaw, extra.Item1);
                            if (accountType == AccountType.Count) {
                                data.extraFn = accounterExtra(extra.Item3);
                            }
                            call(data);
                        }
                    };
                }
            }

            bridge.addDataHandler(source.eventConfigAsTuple(), dataResponseHandler);
        }

        private static Tuple<DataTypeBase, EditorImplBase> findParent(DataProcessor dataprocessor, DataTypeBase child, byte type) {
            if (child.eventConfig[0] == (byte) DATA_PROCESSOR && child.eventConfig[1] == DataProcessor.NOTIFY) {
                var processor = dataprocessor.lookupProcessor(child.eventConfig[2]);
                if (processor.Item2.config[0] == type) {
                    return processor;
                }

                return findParent(dataprocessor, child.input, type);
            }
            return null;
        }

        private static Tuple<DateTime, int, uint> fillTimestamp(IModuleBoardBridge bridge, Tuple<DataTypeBase, EditorImplBase> accounter, byte[] response, int offset) {
            if (accounter != null) {
                byte[] config = accounter.Item2.config;
                if (config[0] == DataProcessor.TYPE_ACCOUNTER) {
                    var configObj = accounter.Item2.configObj as AccounterConfig;
                    uint tick = BitConverter.ToUInt32(response, offset);

                    switch (configObj.type) {
                        case AccountType.Count:
                            return Tuple.Create(DateTime.Now, configObj.length + offset, tick);
                        case AccountType.Time:
                            var logging = bridge.GetModule<ILogging>() as Logging;
                            return Tuple.Create(logging.computeTimestamp(0xff, tick), configObj.length + offset, tick);
                    }
                    
                }
            }
            return Tuple.Create(DateTime.Now, offset, (uint) 0);
        }
    }
}
