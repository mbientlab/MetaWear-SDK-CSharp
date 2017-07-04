namespace MbientLab.MetaWear.Data {
    /// <summary>
    /// Encapsulates angular velocity data, values are in degrees per second
    /// </summary>
    public class AngularVelocity : FloatVector {
        public const char DEGREES= '\u00B0';

        public float X { get { return vector[0]; } }
        public float Y { get { return vector[1]; } }
        public float Z { get { return vector[2]; } }

        public AngularVelocity(float x, float y, float z) : base(x, y, z) { }

        public override string ToString() {
            var dps = string.Format("{0}/s", DEGREES.ToString());
            return string.Format("{{X: {0:F3}{4}, Y: {1:F3}{5}, Z: {2:F3}{6}{3}", X, Y, Z, "}", dps, dps, dps);
        }
    }
}
