namespace MbientLab.MetaWear.Data {
    /// <summary>
    /// Encapsulates a quaternion in the form q = w + x<b>i</b> + y<b>j</b> + z<b>k</b>
    /// </summary>
    public class Quaternion : FloatVector {
        public float W { get { return vector[0]; } }
        public float X { get { return vector[1]; } }
        public float Y { get { return vector[2]; } }
        public float Z { get { return vector[3]; } }

        public Quaternion(float w, float x, float y, float z) : base(w, x, y, z) { }

        public override string ToString() {
            return string.Format("{{W: {4:F6}, X: {0:F6}, Y: {1:F6}, Z: {2:F6}{3}", X, Y, Z, "}", W);
        }
    }
}
