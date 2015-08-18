using System;
using EpimetheusPlugins.Procedural;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class UniformlyDistributedValue
        : BaseValueGenerator
    {
        public UniformlyDistributedValue(float min, float max, bool vary = false)
            : base(new ConstantValue(min), new ConstantValue(max), vary)
        {
        }

        public UniformlyDistributedValue(IValueGenerator min, IValueGenerator max, bool vary = false)
            : base(min, max, vary)
        {
        }

        protected override float GenerateFloatValue(Func<double> random, INamedDataCollection data)
        {
            return random.RandomSingle(Min.SelectFloatValue(random, data), Max.SelectFloatValue(random, data));
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Min { get; set; }
            public float Max { get; set; }
            public bool Vary { get; set; }

            protected override IValueGenerator UnwrapImpl()
            {
                return new UniformlyDistributedValue(Min, Max, Vary);
            }
        }
    }
}
