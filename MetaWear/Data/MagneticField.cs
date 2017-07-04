namespace MbientLab.MetaWear.Data {
    /// <summary>
    /// Encapsulates magnetic field strength data, values are in Tesla (g)
    /// </summary>
    public class MagneticField : FloatVector {
        public float X { get { return vector[0]; } }
        public float Y { get { return vector[1]; } }
        public float Z { get { return vector[2]; } }

        public MagneticField(float x, float y, float z) : base(x, y, z) { }

        public override string ToString() {
            return string.Format("{{X: {0:F9}T, Y: {1:F9}T, Z: {2:F9}T{3}", X, Y, Z, "}");
        }
    }
}
