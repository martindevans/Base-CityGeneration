using System;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class UniformlyDistributedValue
        : BaseValueGenerator
    {
        public UniformlyDistributedValue(float min, float max, bool vary = false)
            : base(min, max, vary)
        {
        }

        protected override float GenerateFloatValue(Func<double> random)
        {
            return random.RandomSingle(Min, Max);
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Min { get; set; }
            public float Max { get; set; }
            public bool Vary { get; set; }

            protected override BaseValueGenerator UnwrapImpl()
            {
                return new UniformlyDistributedValue(Min, Max, Vary);
            }
        }
    }
}
