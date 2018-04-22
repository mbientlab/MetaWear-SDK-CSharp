using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class ModuleInfo {
        [DataMember] readonly internal byte id, implementation, revision;
        [DataMember] readonly internal byte[] extra;

        internal bool Present => implementation != 0xff && revision != 0xff;

        public ModuleInfo(byte[] response) {
            id = response[0];
            if (response.Length > 2) {
                implementation = response[2];
                revision = response[3];
            }
            else {
                implementation = 0xff;
                revision = 0xff;
            }

            if (response.Length > 4) {
                extra = new byte[response.Length - 4];
                Array.Copy(response, 4, extra, 0, extra.Length);
            }
            else {
                extra = new byte[0];
            }
        }

        internal IDictionary Dump() {
            IDictionary output = new OrderedDictionary();

            if (Present) {
                output.Add("implementation", implementation);
                output.Add("revision", revision);

                if (extra.Length > 0) {
                    output.Add("extra", Util.ArrayToHexString(extra));
                }
            }

            return output;
        }
    }
}
