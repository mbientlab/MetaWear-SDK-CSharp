using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace MbientLab.MetaWear.Impl {
    [DataContract]
    class Version : IComparable<Version> {
        private const string VERSION_STRING_PATTERN = @"(\d+)\.(\d+)\.(\d+)";

        [DataMember] private readonly int major, minor, step;

        internal Version(int major, int minor, int step) {
            this.major = major;
            this.minor = minor;
            this.step = step;
        }

        internal Version(string versionStr) {
            Regex rgx = new Regex(VERSION_STRING_PATTERN, RegexOptions.IgnoreCase);
            MatchCollection matches = rgx.Matches(versionStr);

            if (matches.Count > 0 && matches[0].Groups.Count >= 3) {
                major = Int32.Parse(matches[0].Groups[1].Value);
                minor = Int32.Parse(matches[0].Groups[2].Value);
                step = Int32.Parse(matches[0].Groups[3].Value);
            } else {
                major = 0;
                minor = 0;
                step = 0;
            }
        }

        public override string ToString() {
            return string.Format("{0}.{1}.{2}", major, minor, step);
        }

        public int CompareTo(Version that) {
            int weightedCompare(int left, int right) => (left < right) ? -1 : (left > right ? 1 : 0);

            return (that == null) ? 1 : 4 * weightedCompare(major, that.major) + 2 * weightedCompare(minor, that.minor) + weightedCompare(step, that.step);
        }
    }
}
