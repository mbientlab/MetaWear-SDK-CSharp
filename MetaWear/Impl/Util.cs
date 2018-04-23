using System;

namespace MbientLab.MetaWear.Impl {
    public class Util {
        public static string ArrayToHexString(byte[] array) {
            return string.Format("[0x{0}]", BitConverter.ToString(array).ToLower().Replace("-", ", 0x"));
        }

        public static int ClosestIndex_float(float[] values, float key) {
            float smallest = Math.Abs(values[0] - key);
            int place = 0;

            for (int i = 1; i < values.Length; i++) {
                float distance = Math.Abs(values[i] - key);
                if (distance < smallest) {
                    smallest = distance;
                    place = i;
                }
            }

            return place;
        }

        public static int ClosestIndex_ushort(ushort[] values, ushort key) {
            int smallest = values[0] < key ? key - values[0] : values[0] - key;
            int place = 0;

            for (int i = 1; i < values.Length; i++) {
                int distance = values[0] < key ? key - values[0] : values[0] - key;
                if (distance < smallest) {
                    smallest = distance;
                    place = i;
                }
            }

            return place;
        }

        internal static byte setRead(byte value) {
            return (byte)(0x80 | value);
        }

        internal static byte clearRead(byte value) {
            return (byte)(value & 0x3f);
        }

        internal static ushort bytesLeToUshort(byte[] bytes, int offset) {
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt16(bytes, offset);
        }

        internal static short bytesLeToShort(byte[] bytes, int offset) {
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt16(bytes, offset);
        }

        internal static uint bytesLeToUint(byte[] bytes, int offset) {
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt32(bytes, offset);
        }

        internal static int bytesLeToInt(byte[] bytes, int offset) {
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt32(bytes, offset);
        }

        internal static byte[] ushortToBytesLe(ushort value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal static byte[] shortToBytesLe(short value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal static byte[] intToBytesLe(int value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal static byte[] uintToBytesLe(uint value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal static byte[] pad(byte[] bytes, bool signed) {
            byte[] copy = new byte[bytes.Length + 1];
            Array.Copy(bytes, copy, bytes.Length);

            if (signed && (bytes[bytes.Length - 1] & 0x80) == 0x80) {
                copy[copy.Length - 1] = 0xff;
            }
            return copy;
        }
    }
}
