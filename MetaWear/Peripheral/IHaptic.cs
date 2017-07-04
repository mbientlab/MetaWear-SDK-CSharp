namespace MbientLab.MetaWear.Peripheral {
    /// <summary>
    /// Provides haptic feedback in the form of a vibrational motor or buzzer
    /// </summary>
    public interface IHaptic : IModule {
        /// <summary>
        /// Start pulsing a motor
        /// </summary>
        /// <param name="pulseWidth">How long to run the motor, in milliseconds (ms)</param>
        /// <param name="dutyCycle">Strength of the motor, defaults to 100%</param>
        void StartMotor(ushort pulseWidth, float dutyCycle = 100f);
        /// <summary>
        /// Start pulsing a buzzer
        /// </summary>
        /// <param name="pulseWidth">How long to run the buzzer, in milliseconds (ms)</param>
        void StartBuzzer(ushort pulseWidth);
    }
}
