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
                return _mean;
            }
        }

        private readonly IValueGenerator _deviation;
        public IValueGenerator Deviation
        {
            get
            {
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
            Contract.Requires<ArgumentNullException>(min != null, "min != null");
            Contract.Requires<ArgumentNullException>(mean != null, "mean != null");
            Contract.Requires<ArgumentNullException>(max != null, "max != null");
            Contract.Requires<ArgumentNullException>(deviation != null, "deviation != null");

            _mean = mean;
            _deviation = deviation;
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
