using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MbientLab.MetaWear.Data {
    /// <summary>
    /// Generic container holding a vector of float values
    /// </summary>
    public abstract class FloatVector {
        protected readonly float[] vector;

        protected FloatVector(float x0, float x1, float x2) {
            vector = new float[] { x0, x1, x2 };
        }

        protected FloatVector(float x0, float x1, float x2, float x3) {
            vector = new float[] { x0, x1, x2, x3 };
        }

        public float this[int i] {
            get { return vector[i]; }
        }

        public override bool Equals(Object obj) {
            if (this == obj) return true;
            if (obj == null || GetType() != obj.GetType()) return false;

            var that = obj as FloatVector;

            return vector.SequenceEqual(that.vector);
        }

        public override int GetHashCode() {
            return EqualityComparer<float[]>.Default.GetHashCode(vector);
        }
    }
}
