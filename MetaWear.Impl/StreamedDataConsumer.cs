using MbientLab.MetaWear.Builder;
using System;
using System.Runtime.Serialization;

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
                        byte[] dataRaw = new byte[dataUnitLength];

                        DateTime now = DateTime.Now;
                        for (int i = 0, j = source.eventConfig[2] == DataTypeBase.NO_ID ? 2 : 3;
                            i < source.attributes.copies && j < response.Length; i++, j += dataUnitLength) {
                            Array.Copy(response, j, dataRaw, 0, dataUnitLength);
                            call(source.createData(false, bridge, dataRaw, now));
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

                        call(source.createData(false, bridge, dataRaw, DateTime.Now));
                    };
                }
            }

            bridge.addDataHandler(source.eventConfigAsTuple(), dataResponseHandler);
        }
    }
}
