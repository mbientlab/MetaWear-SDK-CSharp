using MbientLab.MetaWear.Peripheral.Led;

namespace MbientLab.MetaWear.Peripheral {
    namespace Led {
        /// <summary>
        /// Available LED colors
        /// </summary>
        public enum Color {
            Green,
            Red,
            Blue
        }
        /// <summary>
        /// Preconfigured LED patterns 
        /// </summary>
        public enum Pattern {
            Blink,
            Pulse,
            Solid
        }
    }
    /// <summary>
    /// Ultra bright RGB light emitting diode
    /// </summary>
    public interface ILed : IModule {
        /// <summary>
        /// Edit the pattern attributes for the desired color.  All parameters except the color are optional and 
        /// if not set, will default to 0 except the count parameter which defaults an indefinite count.
        /// </summary>
        /// <param name="color">Color to configure</param>
        /// <param name="high">Intensity value of the high state</param>
        /// <param name="low">Intensity value of the low state</param>
        /// <param name="riseTime">Transition time from low to high state, in milliseconds (ms)</param>
        /// <param name="highTime">How long the pulse stays in the high state, in milliseconds (ms)</param>
        /// <param name="fallTime">Transition time from high to low state, in milliseconds (ms)</param>
        /// <param name="duration">Length of one pulse, in milliseconds (ms)</param>
        /// <param name="delay">How long to wait before starting the pattern, in milliseconds (ms), ignored on boards running firmware older than v1.2.3</param>
        /// <param name="count">How many times to repeat a pulse pattern</param>
        void EditPattern(Color color, byte high = 0, byte low = 0, 
            ushort riseTime = 0, ushort highTime = 0, ushort fallTime = 0, 
            ushort duration = 0, ushort delay = 0, byte count = 0xff);
        /// <summary>
        /// Apply a preset pattern to the desired color
        /// </summary>
        /// <param name="color">Color to configure</param>
        /// <param name="pattern">Preconfigured preset to use</param>
        /// <param name="delay">Set how long to wait before starting the pattern, defaults to 0.  This setting is ignored 
        /// on boards running firmware older than v1.2.3</param>
        /// <param name="count">Set how many times to repeat a pulse pattern, defaults to indefinite</param>
        void EditPattern(Color color, Pattern pattern, ushort delay = 0, byte count = 0xff);

        /// <summary>
        /// Play any programmed patterns and immediately plays patterns programmed later
        /// </summary>
        void AutoPlay();
        /// <summary>
        /// Play any programmed patterns
        /// </summary>
        void Play();
        /// <summary>
        /// Pause the pattern playback
        /// </summary>
        void Pause();
        /// <summary>
        /// Stop playing LED patterns
        /// </summary>
        /// <param name="clear">True if the patterns should be cleared as well</param>
        void Stop(bool clear);
    }
}
