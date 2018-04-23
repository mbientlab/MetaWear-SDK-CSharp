using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class DataAttributes {
        [DataMember] internal byte[] sizes;
        [DataMember] internal byte copies, offset;
        [DataMember] internal bool signed;

        internal DataAttributes(byte[] sizes, byte copies, byte offset, bool signed) {
            this.sizes = sizes;
            this.copies = copies;
            this.offset = offset;
            this.signed = signed;
        }

        internal DataAttributes dataProcessorCopy() {
            byte[] sizesCopy = new byte[sizes.Length];
            Array.Copy(sizes, sizesCopy, sizes.Length);

            return new DataAttributes(sizesCopy, copies, 0, signed);
        }

        internal DataAttributes dataProcessorCopySize(byte newSize) {
            byte[] sizesCopy = new byte[sizes.Length];
            for (int i = 0; i < sizes.Length; ++i) {
                sizesCopy[i] = newSize;
            }

            return new DataAttributes(sizesCopy, copies, 0, signed);
        }

        internal DataAttributes dataProcessorCopySigned(bool newSigned) {
            byte[] sizesCopy = new byte[sizes.Length];
            Array.Copy(sizes, sizesCopy, sizes.Length);

            return new DataAttributes(sizesCopy, copies, 0, newSigned);
        }

        internal DataAttributes dataProcessorCopyCopies(byte newCopies) {
            byte[] sizesCopy = new byte[sizes.Length];
            Array.Copy(sizes, sizesCopy, sizes.Length);
            return new DataAttributes(sizesCopy, newCopies, 0, signed);
        }

        public byte length() {
            return (byte)(unitLength() * copies);
        }

        public byte unitLength() {
            return sizes.Aggregate((byte) 0, (acc, s) => (byte) (acc + s));
        }

        public override bool Equals(object obj) {
            var attributes = obj as DataAttributes;
            return attributes != null &&
                   sizes.SequenceEqual(attributes.sizes) &&
                   copies == attributes.copies &&
                   offset == attributes.offset &&
                   signed == attributes.signed;
        }

        public override int GetHashCode() {
            var hashCode = 1372441812;
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(sizes);
            hashCode = hashCode * -1521134295 + copies.GetHashCode();
            hashCode = hashCode * -1521134295 + offset.GetHashCode();
            hashCode = hashCode * -1521134295 + signed.GetHashCode();
            return hashCode;
        }
    }
}
