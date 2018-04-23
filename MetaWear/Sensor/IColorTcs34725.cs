using MbientLab.MetaWear.Sensor.ColorTcs34725;
using System;

namespace MbientLab.MetaWear.Sensor {
    namespace ColorTcs34725 {
        /// <summary>
        /// Analog gain multipliers
        /// </summary>
        public enum Gain {
            _1x,
            _4x,
            _16x,
            _60x
        }
        /// <summary>
        /// Wrapper class encapsulating adc data from the sensor
        /// </summary>
        public class Adc {
            public ushort Clear { get; }
            public ushort Red { get; }
            public ushort Green { get; }
            public ushort Blue { get; }

            public Adc(ushort clear, ushort red, ushort green, ushort blue) {
                Clear = clear;
                Red = red;
                Green = green;
                Blue = blue;
            }

            public override string ToString() {
                return string.Format("{{clear: {0:d}, red: {1:d}, green: {2:d}, blue: {3:d}{4}", Clear, Red, Green, Blue, "}");
            }

            public override bool Equals(Object obj) {
                if (this == obj) return true;
                if (obj == null || GetType() != obj.GetType()) return false;

                Adc adc = obj as Adc;

                return Clear == adc.Clear && Red == adc.Red && Green == adc.Green && Blue == adc.Blue;

            }

            public override int GetHashCode() {
                int result = Clear;
                result = 31 * result + Red;
                result = 31 * result + Green;
                result = 31 * result + Blue;
                return result;
            }
        }
    }
    /// <summary>
    /// Colored light-to-digital converter by TAOS that can sense red, green, blue, and clear light
    /// </summary>
    public interface IColorTcs34725 : IModule {
        /// <summary>
        /// Data producer representing the measured adc values
        /// </summary>
        IForcedDataProducer Adc { get; }

        /// <summary>
        /// Configure the color sensor
        /// </summary>
        /// <param name="gain">Analog gain, defaults to 1x</param>
        /// <param name="integationTime">Integration time, impacts resolution and sensitivity, defaults to 2.4ms</param>
        /// <param name="illuminate">True if illuminator led should be flashed before measuring the data, defaults to false</param>
        void Configure(Gain gain = Gain._1x, float integationTime = 2.4f, bool illuminate = false);
    }
}
