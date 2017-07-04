using System;
using System.Collections.Generic;
using System.Text;

namespace MbientLab.MetaWear.Impl {
    public class Util {
        public static string arrayToHexString(byte[] array) {
            var builder = new StringBuilder();

            builder.Append(string.Format("[0x{0:X2}", array[0]));
            for (int i = 1; i < array.Length; i++) {
                builder.Append(string.Format(", 0x{0:X2}", array[i]));
            }
            builder.Append("]");
            return builder.ToString();
        }

        public static int closestIndex(float[] values, float key) {
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

        internal static byte setRead(byte value) {
            return (byte)(0x80 | value);
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
