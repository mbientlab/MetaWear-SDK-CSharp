using MbientLab.MetaWear.Builder;
using MbientLab.MetaWear.Core.DataProcessor;

namespace MbientLab.MetaWear.Core {
    namespace DataProcessor {
        /// <summary>
        /// Common base class for all data processor editors
        /// </summary>
        public interface IEditor {
        }
        /// <summary>
        /// Edits a component
        /// <seealso cref="IRouteComponent.Count"/>
        /// </summary>
        public interface ICounterEditor : IEditor {
            /// <summary>
            /// Reset the internal counter
            /// </summary>
            void Reset();
            /// <summary>
            /// Overwrite the internal counter with a new value
            /// </summary>
            /// <param name="value">New value</param>
            void Set(uint value);
        }
        /// <summary>
        /// Edits an accumulate component
        /// <seealso cref="IRouteComponent.Accumulate"/>
        /// </summary>
        public interface IAccumulatorEditor : IEditor {
            /// <summary>
            /// Reset the running sum
            /// </summary>
            void Reset();
            /// <summary>
            /// Overwrite the accumulated sum with a new value
            /// </summary>
            /// <param name="value">New value</param>
            void Set(float value);
        }
        /// <summary>
        /// Edits a limiter
        /// <seealso cref="IRouteComponent.Limit(Passthrough, ushort)"/>
        /// </summary>
        public interface IPassthroughEditor : IEditor {
            /// <summary>
            /// Set the internal state
            /// </summary>
            /// <param name="value">New value</param>
            void Set(ushort value);
            /// <summary>
            /// Changes the passthrough type and initial value
            /// </summary>
            /// <param name="type">New passthrough type</param>
            /// <param name="value">Initial value of the modified limiter</param>
            void Modify(Passthrough type, ushort value);
        }
        /// <summary>
        /// Edits a 2 op math component 
        /// <seealso cref="IRouteComponent.Map(Function2, float)"/>
        /// </summary>
        public interface IMapEditor : IEditor {
            /// <summary>
            /// Modifies the right hand value used in the computation
            /// </summary>
            /// <param name="rhs">New right hand value</param>
            void ModifyRhs(float rhs);
        }
        /// <summary>
        /// Edits a comparison filter
        /// <seealso cref="IRouteComponent.Filter(Comparison, float[])"/>
        /// <seealso cref="IRouteComponent.Filter(Comparison, ComparisonOutput, float[])"/>
        /// </summary>
        public interface IComparatorEditor : IEditor {
            /// <summary>
            /// Modifies the references values and comparison operation
            /// </summary>
            /// <param name="op">New comparison operation</param>
            /// <param name="references">New reference values, can be multiple values if the board is running firmware v1.2.3 or later</param>
            void Modify(Comparison op, params float[] references);
        }
        /// <summary>
        /// Edits a pulse finder
        /// <seealso cref="IRouteComponent.Find(Pulse, float, ushort)"/>
        /// </summary>
        public interface IPulseEditor : IEditor {
            /// <summary>
            /// Change the criteria that classifies a pulse
            /// </summary>
            /// <param name="threshold">New boundary the data must exceed</param>
            /// <param name="samples">New minimum data sample size</param>
            void Modify(float threshold, ushort samples);
        }
        /// <summary>
        /// Edits a threshold finder
        /// <seealso cref="IRouteComponent.Find(Threshold, float)"/>
        /// </summary>
        public interface IThresholdEditor : IEditor {
            /// <summary>
            /// Modifies the threshold and hysteresis values
            /// </summary>
            /// <param name="threshold">New threshold value</param>
            /// <param name="hysteresis">New hysteresis value</param>
            void Modify(float threshold, float hysteresis);
        }
        /// <summary>
        /// Edits a differential finder
        /// <seealso cref="IRouteComponent.Find(Differential, float)"/>
        /// </summary>
        public interface IDifferentialEditor : IEditor {
            /// <summary>
            /// Modifies the minimum distance from the reference value
            /// </summary>
            /// <param name="distance">New minimum distance value</param>
            void Modify(float distance);
        }
        /// <summary>
        /// Edits a time limiter
        /// <seealso cref="IRouteComponent.Limit(uint)"/>
        /// </summary>
        public interface ITimeEditor : IEditor {
            /// <summary>
            /// Change how often to allow data through
            /// </summary>
            /// <param name="period"></param>
            void Modify(uint period);
        }
        /// <summary>
        /// Edits a high pass filter
        /// <seealso cref="IRouteComponent.HighPass(byte)"/>
        /// </summary>
        public interface IHighPassEditor : IEditor {
            /// <summary>
            /// Change how many samples are used to compute the value
            /// </summary>
            /// <param name="samples">New sample size</param>
            void Modify(byte samples);
            /// <summary>
            /// Reset the running average
            /// </summary>
            void Reset();
        }
        /// <summary>
        /// Edits a low pass filter
        /// <seealso cref="IRouteComponent.LowPass(byte)"/>
        /// </summary>
        public interface ILowPassEditor : IEditor {
            /// <summary>
            /// Change how many samples are used to compute the value
            /// </summary>
            /// <param name="samples">New sample size</param>
            void Modify(byte samples);
            /// <summary>
            /// Reset the running average
            /// </summary>
            void Reset();
        }
        /// <summary>
        /// Edits a data packer
        /// <seealso cref="IRouteComponent.Pack(byte)"/>
        /// </summary>
        public interface IPackerEditor : IEditor {
            /// <summary>
            /// Clears buffer of accumulated inputs
            /// </summary>
            void Clear();
        }
    }

    /// <summary>
    /// Firmware feature that manipulates data on-board
    /// </summary>
    public interface IDataProcessor : IModule {
        /// <summary>
        /// Edits a data processor
        /// </summary>
        /// <typeparam name="T">Type to cast the returned object as</typeparam>
        /// <param name="name">Processor name to look up, set by <see cref="IRouteComponent.Name(string)"/></param>
        /// <returns>Editor object to modify the processor</returns>
        T Edit<T>(string name) where T : class, IEditor;
        /// <summary>
        /// Gets a <see cref="IForcedDataProducer"/> representing the processor's internal state
        /// </summary>
        /// <param name="name">Processor name to look up, set by <see cref="IRouteComponent.Name(string)"/></param>
        /// <returns>Data producer object, null if the processor does not have an internal state</returns>
        IForcedDataProducer State(string name);
    }
}
