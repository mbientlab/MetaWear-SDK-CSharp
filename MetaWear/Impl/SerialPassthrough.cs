using MbientLab.MetaWear.Peripheral;
using static MbientLab.MetaWear.Impl.Module;
using System;
using System.Collections.Generic;
using MbientLab.MetaWear.Builder;
using System.Threading.Tasks;
using MbientLab.MetaWear.Peripheral.SerialPassthrough;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    internal class SpiParameterBuilder {
        byte originalLength;
        byte[] config;

        internal SpiParameterBuilder() {
            originalLength = 5;
            config = new byte[originalLength];
        }

        internal SpiParameterBuilder(byte fifthValue) {
            originalLength = 6;
            config = new byte[originalLength];
            config[5] = fifthValue;
        }

        public SpiParameterBuilder data(byte[] data) {
            byte[] copy = new byte[config.Length + data.Length];
            Array.Copy(config, 0, copy, 0, originalLength);
            Array.Copy(data, 0, copy, originalLength, data.Length);
            config = copy;
            return this;
        }

        public SpiParameterBuilder slaveSelectPin(byte pin) {
            config[0] = pin;
            return this;
        }
     
        public SpiParameterBuilder clockPin(byte pin) {
            config[1] = pin;
            return this;
        }

        public SpiParameterBuilder mosiPin(byte pin) {
            config[2] = pin;
            return this;
        }

        public SpiParameterBuilder misoPin(byte pin) {
            config[3] = pin;
            return this;
        }

        
        public SpiParameterBuilder lsbFirst() {
            config[4] |= 0x1;
            return this;
        }

        
        public SpiParameterBuilder mode(byte mode) {
            config[4] |= (byte) (mode << 1);
            return this;
        }

        public SpiParameterBuilder frequency(SpiFrequency freq) {
            config[4] |= (byte) ((byte) freq << 3);
            return this;
        }

        public SpiParameterBuilder useNativePins() {
            config[4] |= (0x1 << 6);
            return this;
        }

        public byte[] build() {
            return config;
        }
    }

    [DataContract]
    class SerialPassthroughData : ByteArrayDataType {
        internal SerialPassthroughData(byte register, byte id, byte length) : base (SERIAL_PASSTHROUGH, register, id, new DataAttributes(new byte[] { length }, 1, 0, false)) { }
    }

    [KnownType(typeof(SPIDataProducer))]
    [KnownType(typeof(I2CDataProducer))]
    [DataContract]
    class SerialPassthrough : ModuleImplBase, ISerialPassthrough {
        internal static string createIdentifier(DataTypeBase dataType) {
            switch (Util.clearRead(dataType.eventConfig[1])) {
                case I2C_RW:
                    return string.Format("i2c[{0}]", dataType.eventConfig[2]);
                case SPI_RW:
                    return string.Format("spi[{0}]", dataType.eventConfig[2]);
                default:
                    return null;
            }
        }

        private const byte SPI_REVISION = 1;
        private const byte I2C_RW = 0x1, SPI_RW = 0x2, DIRECT_I2C_READ_ID = 0xff, DIRECT_SPI_READ_ID = 0xf;

        [KnownType(typeof(SerialPassthroughData))]
        [DataContract]
        private class I2CDataProducer : SerializableType, II2CDataProducer {
            [DataMember] internal readonly DataTypeBase i2cByteArray;
            [DataMember] private readonly byte id;

            public I2CDataProducer(byte id, byte length, IModuleBoardBridge bridge) : base(bridge) {
                this.id = id;
                i2cByteArray = new SerialPassthroughData(Util.setRead(I2C_RW), id, length);
            }

            public Task<IRoute> AddRouteAsync(Action<IRouteComponent> builder) {
                return bridge.queueRouteBuilder(builder, i2cByteArray);
            }

            public void Read(byte deviceAddr, byte registerAddr) {
                bridge.sendCommand(SERIAL_PASSTHROUGH, i2cByteArray.eventConfig[1], new byte[] { deviceAddr, registerAddr, id, i2cByteArray.attributes.length() });
            }
        }

        [KnownType(typeof(SerialPassthroughData))]
        [DataContract]
        private class SPIDataProducer : SerializableType, ISPIDataProducer {
            [DataMember] internal readonly DataTypeBase spiByteArray;
            [DataMember] private readonly byte id;

            public SPIDataProducer(byte id, byte length, IModuleBoardBridge bridge) : base(bridge) {
                this.id = id;
                spiByteArray = new SerialPassthroughData(Util.setRead(SPI_RW), id, length);
            }

            public Task<IRoute> AddRouteAsync(Action<IRouteComponent> builder) {
                return bridge.queueRouteBuilder(builder, spiByteArray);
            }

            public void Read(byte slaveSelectPin, byte clockPin, byte mosiPin, byte misoPin, byte mode, SpiFrequency frequency,
                    byte[] data = null, bool lsbFirst = true, bool useNativePins = true) {
                SpiParameterBuilder builder = new SpiParameterBuilder((byte)((spiByteArray.attributes.length() - 1) | (id << 4)));
                builder.slaveSelectPin(slaveSelectPin)
                    .clockPin(clockPin)
                    .mosiPin(mosiPin)
                    .misoPin(misoPin)
                    .mode(mode)
                    .frequency(frequency);

                if (lsbFirst) {
                    builder.lsbFirst();
                }

                if (useNativePins) {
                    builder.useNativePins();
                }

                if (data != null) {
                    builder.data(data);
                }

                byte[] parameters = builder.build();
                byte[] command = new byte[spiByteArray.eventConfig.Length - 1 + parameters.Length];
                Array.Copy(spiByteArray.eventConfig, 0, command, 0, 2);
                Array.Copy(parameters, 0, command, 2, parameters.Length);

                bridge.sendCommand(command);
            }
        }

        [DataMember] private Dictionary<Byte, I2CDataProducer> i2cProducers = new Dictionary<Byte, I2CDataProducer>();
        [DataMember] private Dictionary<Byte, SPIDataProducer> spiProducers = new Dictionary<Byte, SPIDataProducer>();

        private TimedTask<byte[]> spiReadTask, i2cReadTask;

        public SerialPassthrough(IModuleBoardBridge bridge) : base(bridge) {
        }

        internal override void aggregateDataType(ICollection<DataTypeBase> collection) {
            foreach(var it in i2cProducers.Values) {
                collection.Add(it.i2cByteArray);
            }

            foreach (var it in spiProducers.Values) {
                collection.Add(it.spiByteArray);
            }
        }

        internal override void restoreTransientVars(IModuleBoardBridge bridge) {
            base.restoreTransientVars(bridge);

            foreach(var it in i2cProducers) {
                it.Value.restoreTransientVars(bridge);
            }
            foreach (var it in spiProducers) {
                it.Value.restoreTransientVars(bridge);
            }
        }

        protected override void init() {
            spiReadTask = new TimedTask<byte[]>();
            i2cReadTask = new TimedTask<byte[]>();

            bridge.addDataIdHeader(Tuple.Create((byte) SERIAL_PASSTHROUGH, Util.setRead(I2C_RW)));
            bridge.addDataHandler(Tuple.Create((byte)SERIAL_PASSTHROUGH, Util.setRead(I2C_RW), DIRECT_I2C_READ_ID), response => i2cReadTask.SetResult(response));

            bridge.addDataIdHeader(Tuple.Create((byte)SERIAL_PASSTHROUGH, Util.setRead(SPI_RW)));
            bridge.addDataHandler(Tuple.Create((byte)SERIAL_PASSTHROUGH, Util.setRead(SPI_RW), DIRECT_SPI_READ_ID), response => spiReadTask.SetResult(response));
        }

        public II2CDataProducer I2C(byte id, byte length) {
            if (i2cProducers.TryGetValue(id, out I2CDataProducer producer)) {
                return producer;
            }

            producer = new I2CDataProducer(id, length, bridge);
            i2cProducers.Add(id, producer);
            return producer;
        }

        public ISPIDataProducer SPI(byte id, byte length) {
            if (bridge.lookupModuleInfo(SERIAL_PASSTHROUGH).revision < SPI_REVISION) {
                return null;
            }
            if (spiProducers.TryGetValue(id, out SPIDataProducer producer)) {
                return producer;
            }

            producer = new SPIDataProducer(id, length, bridge);
            spiProducers.Add(id, producer);
            return producer;
        }

        public void WriteI2C(byte deviceAddr, byte registerAddr, byte[] data) {
            byte[] config= new byte[data.Length + 4];
            config[0]= deviceAddr;
            config[1]= registerAddr;
            config[2]= 0xff;
            config[3]= (byte) data.Length;
            Array.Copy(data, 0, config, 4, data.Length);

            bridge.sendCommand(SERIAL_PASSTHROUGH, I2C_RW, config);
        }

        public async Task<byte[]> ReadI2CAsync(byte deviceAddr, byte registerAddr, byte length) {
            var response = await i2cReadTask.Execute("Did not received I2C data within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(new byte[] { (byte)SERIAL_PASSTHROUGH, Util.setRead(I2C_RW), deviceAddr, registerAddr, DIRECT_I2C_READ_ID, length }));

            if (response.Length > 3) {
                byte[] data = new byte[response.Length - 3];
                Array.Copy(response, 3, data, 0, data.Length);
                return data;
            } else {
                throw new InvalidOperationException("Error reading I2C data from device or register address.  Response: " + Util.ArrayToHexString(response));
            }
        }

        public void WriteSPI(byte slaveSelectPin, byte clockPin, byte mosiPin, byte misoPin, byte mode, SpiFrequency frequency,
                byte[] data, bool lsbFirst = true, bool useNativePins = true) {
            if (bridge.lookupModuleInfo(SERIAL_PASSTHROUGH).revision >= SPI_REVISION) {
                SpiParameterBuilder builder = new SpiParameterBuilder();
                builder.slaveSelectPin(slaveSelectPin)
                    .clockPin(clockPin)
                    .mosiPin(mosiPin)
                    .misoPin(misoPin)
                    .mode(mode)
                    .frequency(frequency);

                if (lsbFirst) {
                    builder.lsbFirst();
                }

                if (useNativePins) {
                    builder.useNativePins();
                }

                if (data != null) {
                    builder.data(data);
                }

                bridge.sendCommand(SERIAL_PASSTHROUGH, SPI_RW, builder.build());
            }
        }

        public async Task<byte[]> ReadSPIAsync(byte length, byte slaveSelectPin, byte clockPin, byte mosiPin, byte misoPin, byte mode, SpiFrequency frequency,
                byte[] data = null, bool lsbFirst = true, bool useNativePins = true) {
            SpiParameterBuilder builder = new SpiParameterBuilder((byte)((length - 1) | (DIRECT_SPI_READ_ID << 4)));
            builder.slaveSelectPin(slaveSelectPin)
                    .clockPin(clockPin)
                    .mosiPin(mosiPin)
                    .misoPin(misoPin)
                    .mode(mode)
                    .frequency(frequency);

            if (lsbFirst) {
                builder.lsbFirst();
            }
            if (useNativePins) {
                builder.useNativePins();
            }
            if (data != null) {
                builder.data(data);
            }

            var response = await spiReadTask.Execute("Did not received SPI data within {0}ms", bridge.TimeForResponse,
                () => bridge.sendCommand(SERIAL_PASSTHROUGH, Util.setRead(SPI_RW), builder.build()));

            if (response.Length > 3) {
                byte[] dataInner = new byte[response.Length - 3];
                Array.Copy(response, 3, dataInner, 0, dataInner.Length);
                return dataInner;
            } else {
                throw new InvalidOperationException("Error reading SPI data from device or register address.  Response: " + Util.ArrayToHexString(response));
            }
        }
    }
}
