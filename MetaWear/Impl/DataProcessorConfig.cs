using MbientLab.MetaWear.Builder;
using System;

namespace MbientLab.MetaWear.Impl {
    abstract class DataProcessorConfig {
        internal static DataProcessorConfig from(Version firmware, byte revision, byte[] config) {
            switch (config[0]) {
                case PassthroughConfig.ID:
                    return new PassthroughConfig(config);
                case AccumulatorConfig.ID:
                    return new AccumulatorConfig(config);
                case AverageConfig.ID:
                    return new AverageConfig(config);
                case ComparisonConfig.ID:
                    return firmware.CompareTo(RouteComponent.MULTI_COMPARISON_MIN_FIRMWARE) >= 0 ?
                            new MultiValueComparisonConfig(config) as ComparisonConfig : 
                            new SingleValueComparisonConfig(config) as ComparisonConfig;
                case CombinerConfig.ID:
                    return new CombinerConfig(config);
                case TimeConfig.ID:
                    return new TimeConfig(config);
                case MathConfig.ID:
                    return new MathConfig(firmware.CompareTo(RouteComponent.MULTI_CHANNEL_MATH) >= 0, config);
                case DelayConfig.ID:
                    return new DelayConfig(revision >= DataProcessor.EXPANDED_DELAY, config);
                case PulseConfig.ID:
                    return new PulseConfig(config);
                case DifferentialConfig.ID:
                    return new DifferentialConfig(config);
                case ThresholdConfig.ID:
                    return new ThresholdConfig(config);
                case BufferConfig.ID:
                    return new BufferConfig(config);
                case PackerConfig.ID:
                    return new PackerConfig(config);
                case AccounterConfig.ID:
                    return new AccounterConfig(config);
            }
            throw new InvalidOperationException("Unrecognized config id: " + config[0]);
        }

        internal readonly byte id;

        internal DataProcessorConfig(byte id) {
            this.id = id;
        }

        internal abstract byte[] Build();
        internal abstract string CreateIdentifier(bool state, byte procId);

        internal class PassthroughConfig : DataProcessorConfig {
            internal const byte ID = 0x1;

            internal readonly Passthrough type;
            internal readonly ushort value;

            internal PassthroughConfig(byte[] config) : base(config[0]) {
                type = (Passthrough) (config[1] & 0x7);
                value = Util.bytesLeToUshort(config, 2);
            }

            internal PassthroughConfig(Passthrough type, ushort value) : base(ID) {
                this.type = type;
                this.value = value;
            }

            internal override byte[] Build() {
                byte[] config = new byte[4] { ID, (byte)(((int)type) & 0x7), 0, 0 };
                Array.Copy(Util.ushortToBytesLe(value), 0, config, 2, 2);

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("passthrough{0}?id={1}", state ? "-state" : "", procId);
            }
        }

        internal class AccumulatorConfig : DataProcessorConfig {
            internal const byte ID = 0x2;

            internal readonly bool counter;
            internal readonly byte output, input;

            internal AccumulatorConfig(byte[] config) : base(config[0]) {
                counter = (config[1] & 0x10) == 0x10;
                output = (byte)((config[1] & 0x3) + 1);
                input = (byte)(((config[1] >> 2) & 0x3) + 1);
            }

            internal AccumulatorConfig(bool counter, byte output, byte input) : base(ID) {
                this.counter = counter;
                this.output = output;
                this.input = input;
            }

            internal override byte[] Build() {
                return new byte[] { 0x2, (byte)(((output - 1) & 0x3) | (((input - 1) & 0x3) << 2) | (counter ? 0x10 : 0)) };
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("{0}{1}?id={2}", counter ? "count" : "accumulate", state ? "-state" : "", procId);
            }
        }

        internal class AverageConfig : DataProcessorConfig {
            internal const byte ID = 0x3;

            internal readonly byte output, input, samples, nInputs;
            internal readonly bool hpf, supportsHpf;

            internal AverageConfig(byte[] config) : base(config[0]) {
                output = (byte)((config[1] & 0x3) + 1);
                input = (byte)(((config[1] >> 2) & 0x3) + 1);
                samples = config[2];

                if (config.Length == 4) {
                    nInputs = config[3];
                    hpf = (config[1] >> 5) == 1;
                    supportsHpf = true;
                }
            }

            internal AverageConfig(DataAttributes attributes, byte samples, bool hpf, bool supportsHpf) : base(ID) {
                output = attributes.length();
                input = attributes.length();
                this.samples = samples;
                nInputs = (byte)attributes.sizes.Length;
                this.hpf = hpf;
                this.supportsHpf = supportsHpf;
            }

            internal override byte[] Build() {
                byte[] config = new byte[supportsHpf ? 4 : 3];
                config[0] = ID;
                config[1] = (byte)(((output - 1) & 0x3) | (((input - 1) & 0x3) << 2) | ((supportsHpf ? (hpf ? 1 : 0) : 0) << 5));
                config[2] = samples;
                if (supportsHpf) {
                    config[3] = (byte)(nInputs - 1);
                }

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("{0}?id={1}", hpf ? "high-pass" : "low-pass", procId);
            }
        }

        internal abstract class ComparisonConfig : DataProcessorConfig {
            internal const byte ID = 0x6;

            internal ComparisonConfig() : base(ID) {
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("comparison?id={0}", procId);
            }
        }
        internal class MultiValueComparisonConfig : ComparisonConfig {
            internal readonly byte input;
            internal readonly byte[] references;
            internal readonly Comparison op;
            internal readonly ComparisonOutput mode;
            internal readonly bool isSigned;

            internal MultiValueComparisonConfig(bool isSigned, byte input, Comparison op, ComparisonOutput mode, byte[] references) : base() {
                this.isSigned = isSigned;
                this.input = input;
                this.op = op;
                this.mode = mode;
                this.references = references;
            }

            internal MultiValueComparisonConfig(byte[] config) : base() {
                isSigned = (config[1] & 0x1) == 0x1;
                input = (byte)(((input >> 1) & 0x3) + 1);
                op = (Comparison) ((config[1] >> 3) & 0x7);
                mode = (ComparisonOutput) ((config[1] >> 6) & 0x3);

                references = new byte[config.Length - 2];
                Array.Copy(config, 2, references, 0, references.Length);
            }

            internal override byte[] Build() {
                byte[] config = new byte[2 + references.Length];
                config[0] = ID;
                config[1] = (byte)((isSigned ? 1 : 0) | ((input - 1) << 1) | ((byte) op << 3) | ((byte) mode << 6));
                Array.Copy(references, 0, config, 2, references.Length);

                return config;
            }
        }
        internal class SingleValueComparisonConfig : ComparisonConfig {
            internal readonly bool isSigned;
            internal readonly Comparison op;
            internal readonly int reference;

            internal SingleValueComparisonConfig(bool isSigned, Comparison op, int reference) : base() {
                this.isSigned = isSigned;
                this.op = op;
                this.reference = reference;
            }

            internal SingleValueComparisonConfig(byte[] config) : base() {
                isSigned = config[1] == 0x1;
                op = (Comparison) config[2];
                reference = Util.bytesLeToInt(config, 4);
            }

            internal override byte[] Build() {
                byte[] config = new byte[] { ID, (byte)(isSigned ? 1 : 0), (byte)op, 0, 0, 0, 0, 0 };
                byte[] buffer = Util.intToBytesLe(reference);
                Array.Copy(buffer, 0, config, 4, buffer.Length);

                return buffer;
            }
        }

        internal class CombinerConfig : DataProcessorConfig {
            internal const byte ID = 0x7;

            internal readonly byte output, input, nInputs;
            internal readonly bool isSigned, rss;

            internal CombinerConfig(DataAttributes attributes, bool rss) : base(ID) {
                output = attributes.sizes[0];
                input = attributes.sizes[0];
                nInputs = (byte)attributes.sizes.Length;
                isSigned = attributes.signed;
                this.rss = rss;
            }

            internal CombinerConfig(byte[] config) : base(config[0]) {
                output = (byte)((config[1] & 0x3) + 1);
                input = (byte)(((config[1] >> 2) & 0x3) + 1);
                nInputs = (byte)(((config[1] >> 4) & 0x3) + 1);
                isSigned = (config[1] & 0x80) == 0x80;
                rss = config[2] == 1;
            }

            internal override byte[] Build() {
                return new byte[] {
                    ID,
                    (byte) (((output - 1) & 0x3) | (((input - 1) & 0x3) << 2) | (((nInputs - 1) & 0x3) << 4) | (isSigned ? 0x80 : 0)),
                    (byte) (rss ? 1 : 0)
                };
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("{0}?id={1}", rss ? "rss" : "rms", procId);
            }
        }

        internal class TimeConfig : DataProcessorConfig {
            internal const byte ID = 0x8;

            internal readonly byte input, type;
            internal readonly uint period;

            internal TimeConfig(byte input, byte type, uint period) : base(ID) {
                this.input = input;
                this.type = type;
                this.period = period;
            }

            internal TimeConfig(byte[] config) : base(config[0]) {
                period = Util.bytesLeToUint(config, 2);
                input = (byte)((config[1] & 0x7) + 1);
                type = (byte)((config[1] >> 3) & 0x7);
            }

            internal override byte[] Build() {
                byte[] config = new byte[] {
                    ID, (byte) ((input - 1) & 0x7 | (type << 3)), 0, 0, 0, 0
                };
                byte[] buffer = Util.uintToBytesLe(period);

                Array.Copy(buffer, 0, config, 2, buffer.Length);
                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("time?id={0}", procId);
            }
        }

        internal class MathConfig : DataProcessorConfig {
            internal const byte ID = 0x9;

            internal enum Operation {
                Add,
                Multiply,
                Divide,
                Modulus,
                Exponent,
                Sqrt,
                LeftShift,
                RightShift,
                Subtract,
                AbsValue,
                Constant
            }

            internal byte output;
            internal readonly byte input, nInputs;
            internal readonly bool isSigned, multiChnlMath;
            internal readonly Operation op;
            internal int rhs;

            internal MathConfig(DataAttributes attributes, bool multiChnlMath, Operation op, int rhs) : base(ID) {
                output = 0xff;
                input = attributes.sizes[0];
                nInputs = (byte)attributes.sizes.Length;
                isSigned = attributes.signed;
                this.multiChnlMath = multiChnlMath;
                this.op = op;
                this.rhs = rhs;
            }

            internal MathConfig(bool multiChnlMath, byte[] config) : base(config[0]) {
                output = (byte)((config[1] & 0x3) + 1);
                input = (byte)(((config[1] >> 2) & 0x3) + 1);
                isSigned = (config[1] & 0x10) == 0x10;
                op = (Operation) (config[2] - 1);
                rhs = Util.bytesLeToInt(config, 3);

                if (multiChnlMath) {
                    this.multiChnlMath = true;
                    nInputs = (byte)(config[7] + 1);
                } else {
                    this.multiChnlMath = false;
                }
            }

            internal override byte[] Build() {
                if (output == 0xff) {
                    throw new InvalidOperationException("Output length cannot be 0xff");
                }

                byte[] config = new byte[multiChnlMath ? 8 : 7];
                config[0] = ID;
                config[1] = (byte)((output - 1) & 0x3 | ((input - 1) << 2) | (isSigned ? 0x10 : 0));
                config[2] = (byte) ((byte) op + 1);

                byte[] buffer = Util.intToBytesLe(rhs);
                Array.Copy(buffer, 0, config, 3, buffer.Length);
                
                if (multiChnlMath) {
                    config[7] = (byte)(nInputs - 1);
                }

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("math?id={0}", procId);
            }
        }

        internal class DelayConfig : DataProcessorConfig {
            internal const byte ID = 0xa;

            internal readonly bool enhancedDelay;
            internal readonly byte input, samples;

            internal DelayConfig(bool enhancedDelay, byte input, byte samples) : base(ID) {
                this.enhancedDelay = enhancedDelay;
                this.input = input;
                this.samples = samples;
            }

            internal DelayConfig(bool enhancedDelay, byte[] config) : base(config[0]) {
                this.enhancedDelay = enhancedDelay;
                input = (byte)((config[1] & (enhancedDelay ? 0xf : 0x3)) + 1);
                samples = config[2];
            }

            internal override byte[] Build() {
                return new byte[] { ID, (byte)((input - 1) & (enhancedDelay ? 0xf : 0x3)), samples };
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("delay?id={0}", procId);
            }
        }

        internal class PulseConfig : DataProcessorConfig {
            internal const byte ID = 0xb;

            internal readonly byte input;
            internal readonly int threshold;
            internal readonly ushort samples;
            internal readonly Pulse mode;

            internal PulseConfig(byte input, int threshold, ushort samples, Pulse mode) : base(ID) {
                this.input = input;
                this.threshold = threshold;
                this.samples = samples;
                this.mode = mode;
            }

            internal PulseConfig(byte[] config) : base(config[0]) {
                input = (byte)(config[1] + 1);
                threshold = Util.bytesLeToInt(config, 4);
                samples = Util.bytesLeToUshort(config, 8);
                mode = (Pulse) config[3];
            }

            internal override byte[] Build() {
                byte[] config = new byte[10];
                config[0] = ID;
                config[1] = (byte)(input - 1);
                config[2] = 0;
                config[3] = (byte)mode;

                byte[] buffer = Util.intToBytesLe(threshold);
                Array.Copy(buffer, 0, config, 4, buffer.Length);

                buffer = Util.ushortToBytesLe(samples);
                Array.Copy(buffer, 0, config, 8, buffer.Length);

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("pulse?id={0}", procId);
            }
        }

        internal class DifferentialConfig : DataProcessorConfig {
            internal const byte ID = 0xc;

            internal readonly byte input;
            internal readonly bool isSigned;
            internal readonly Differential mode;
            internal readonly int delta;

            internal DifferentialConfig(DataAttributes attributes, Differential mode, int delta) : base(ID) {
                input = attributes.length();
                isSigned = attributes.signed;
                this.mode = mode;
                this.delta = delta;
            }

            internal DifferentialConfig(byte[] config) : base(config[0]) {
                input = (byte)((config[1] & 0x3) + 1);
                isSigned = (config[1] & 0x4) == 0x4;
                mode = (Differential) ((config[1] >> 3) & 0x7);
                delta = Util.bytesLeToInt(config, 2);
            }

            internal override byte[] Build() {
                byte[] config = new byte[6];
                config[0] = ID;
                config[1] = (byte)(((input - 1) & 0x3) | (isSigned ? 0x4 : 0) | ((byte) mode << 3));

                byte[] buffer = Util.intToBytesLe(delta);
                Array.Copy(buffer, 0, config, 2, buffer.Length);

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("differential?id={0}", procId);
            }
        }

        internal class ThresholdConfig : DataProcessorConfig {
            internal const byte ID = 0xd;

            internal readonly byte input;
            internal readonly bool isSigned;
            internal readonly Threshold mode;
            internal readonly int boundary;
            internal readonly short hysteresis;

            internal ThresholdConfig(DataAttributes attributes, Threshold mode, int boundary, short hysteresis) : base(ID) {
                input = attributes.length();
                isSigned = attributes.signed;
                this.mode = mode;
                this.boundary = boundary;
                this.hysteresis = hysteresis;
            }

            internal ThresholdConfig(byte[] config) : base(config[0]) {
                input = (byte)((config[1] & 0x3) + 1);
                isSigned = (config[1] & 0x4) == 0x4;
                mode = (Threshold) ((config[1] >> 3) & 0x7);
                boundary = Util.bytesLeToInt(config, 2);
                hysteresis = Util.bytesLeToShort(config, 6);
            }

            internal override byte[] Build() {
                byte[] config = new byte[8];
                config[0] = ID;
                config[1] = (byte)((input - 1) & 0x3 | (isSigned ? 0x4 : 0) | ((byte) mode << 3));

                byte[] buffer = Util.intToBytesLe(boundary);
                Array.Copy(buffer, 0, config, 2, buffer.Length);

                buffer = Util.shortToBytesLe(hysteresis);
                Array.Copy(buffer, 0, config, 6, buffer.Length);

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("threshold?id={0}", procId);
            }
        }

        internal class BufferConfig : DataProcessorConfig {
            internal const byte ID = 0xf;

            internal readonly byte input;

            internal BufferConfig(byte input) : base(ID) {
                this.input = input;
            }

            internal BufferConfig(byte[] config) :base(config[0]) {
                input = (byte)((config[1] & 0x1f) + 1);
            }

            internal override byte[] Build() {
                return new byte[] { ID, (byte)((input - 1) & 0x1f) };
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("buffer{0}?id={1}", state ? "-state" : "", procId);
            }
        }

        internal class PackerConfig : DataProcessorConfig {
            internal const byte ID = 0x10;

            internal readonly byte input, count;

            internal PackerConfig(byte input, byte count) : base(ID) {
                this.input = input;
                this.count = count;
            }

            internal PackerConfig(byte[] config) : base(config[0]) {
                input = (byte)((config[1] & 0x1f) + 1);
                count = (byte)((config[2] & 0x1f) + 1);
            }

            internal override byte[] Build() {
                return new byte[] { ID, (byte)((input - 1) & 0x1f), (byte)((count - 1) & 0x1f) };
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("packer?id={0}", procId);
            }
        }

        internal class AccounterConfig : DataProcessorConfig {
            internal const byte ID = 0x11;

            internal readonly byte length;
            internal readonly AccountType type;

            internal AccounterConfig(AccountType type, byte length) : base(ID) {
                this.type = type;
                this.length = length;
            }

            internal AccounterConfig(byte[] config) : base(ID) {
                type = (AccountType) (config[1] & 0xf);
                length = (byte)(((config[1] >> 4) & 0x3) + 1);
            }

            internal override byte[] Build() {
                return new byte[] { ID, (byte)((byte) type | ((length - 1) << 4)), 0x3 };
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return string.Format("account?id={0}", procId);
            }
        }

        internal class FuserConfig : DataProcessorConfig {
            internal const byte ID = 0x1b;

            internal readonly string[] names;
            internal readonly byte[] filterIds;

            internal FuserConfig(string[] names) :base(ID) {
                filterIds = new byte[names.Length];
                this.names = names;
            }

            internal FuserConfig(byte[] config) : base(config[0]) {
                names = null;
                filterIds = new byte[config[1] & 0x1f];
                Array.Copy(config, 1, filterIds, 0, filterIds.Length);
            }

            internal void SyncFilterIds(DataProcessor dpModule) {
                int i = 0;
                foreach(var _ in names) {
                    if (dpModule.nameToId.TryGetValue(_, out var id)) {
                        var value = dpModule.activeProcessors[id];
                        if (!(value.Item2.configObj is BufferConfig)) {
                            throw new IllegalRouteOperationException($"Can only use buffer processors as inputs to the fuser (${_})");
                        }

                        filterIds[i] = id;
                        i++;
                    } else {
                        throw new IllegalRouteOperationException($"No buffer named '${_}' exists");
                    }
                }
            }

            internal override byte[] Build() {
                byte[] config = new byte[2 + filterIds.Length];
                config[0] = ID;
                config[1] = (byte) filterIds.Length;
                Array.Copy(filterIds, 0, config, 2, filterIds.Length);

                return config;
            }

            internal override string CreateIdentifier(bool state, byte procId) {
                return $"fuser?id={procId}";
            }
        }
    }
}
