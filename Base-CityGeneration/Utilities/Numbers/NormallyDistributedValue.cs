using EpimetheusPlugins.Procedural;
using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class NormallyDistributedValue
        : BaseValueGenerator
    {
        private readonly IValueGenerator _mean;
        public IValueGenerator Mean
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _mean;
            }
        }

        private readonly IValueGenerator _deviation;
        public IValueGenerator Deviation
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _deviation;
            }
        }

        public NormallyDistributedValue(float min, float mean, float max, float deviation)
            : base(new ConstantValue(min), new ConstantValue(max))
        {
            _mean = new ConstantValue(mean);
            _deviation = new ConstantValue(deviation);
        }

        public NormallyDistributedValue(IValueGenerator min, IValueGenerator mean, IValueGenerator max, IValueGenerator deviation)
            : base(min, max)
        {
            Contract.Requires(min != null);
            Contract.Requires(mean != null);
            Contract.Requires(max != null);
            Contract.Requires(deviation != null);

            _mean = mean;
            _deviation = deviation;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_deviation != null);
            Contract.Invariant(_mean != null);
        }

        protected override float GenerateFloatValue(Func<double> random, INamedDataCollection data)
        {
            return random.NormallyDistributedSingle(
                Deviation.SelectFloatValue(random, data),
                Mean.SelectFloatValue(random, data),
                Min.SelectFloatValue(random, data),
                Max.SelectFloatValue(random, data)
            );
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Min { get; set; }
            public float? Mean { get; set; }
            public float Max { get; set; }
            public float? Deviation { get; set; }
            public bool Vary { get; set; }

            protected override IValueGenerator UnwrapImpl()
            {

                var mean = Mean ?? MeanCalc(Min, Max);
                var deviation = Deviation ?? DeviationCalc(Min, Max);

                return new NormallyDistributedValue(Min, mean, Max, deviation).Transform(vary: Vary);
            }

            private static float MeanCalc(float min, float max)
            {
                return min * 0.5f + max * 0.5f;
            }

            private static float DeviationCalc(float min, float max)
            {
                return (max - min) * 0.2f;
            }
        }
    }
}
