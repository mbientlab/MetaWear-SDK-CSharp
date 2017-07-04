namespace MbientLab.MetaWear.Data {
    /// <summary>
    /// Encapsulates Euler angles, values are in degrees
    /// </summary>
    public class EulerAngles : FloatVector {
        public float Heading { get { return vector[0]; } }
        public float Pitch { get { return vector[1]; } }
        public float Roll { get { return vector[2]; } }
        public float Yaw { get { return vector[3]; } }

        public EulerAngles(float heading, float pitch, float roll, float yaw) : base(heading, pitch, roll, yaw) { }

        public override string ToString() {
            return string.Format("{{Heading: {4:F3}, Pitch: {0:F3}, Roll: {1:F3}, Yaw: {2:F3}{3}", Heading, Pitch, Roll, "}", Yaw);
        }
    }
}
