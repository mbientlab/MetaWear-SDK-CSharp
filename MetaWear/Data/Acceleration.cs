namespace MbientLab.MetaWear.Data {
    /// <summary>
    /// Encapsulates acceleration data, values are represented in g's
    /// </summary>
    public class Acceleration : FloatVector {
        public float X { get { return vector[0]; } }
        public float Y { get { return vector[1]; } }
        public float Z { get { return vector[2]; } }

        public Acceleration(float x, float y, float z) : base(x, y, z) { }

        public override string ToString() {
            return string.Format("{{X: {0:F3}g, Y: {1:F3}g, Z: {2:F3}g{3}", X, Y, Z, "}");
        }
    }
}
