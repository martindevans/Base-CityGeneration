using System;
using System.Diagnostics.Contracts;
using EpimetheusPlugins.Procedural;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class UniformlyDistributedValue
        : BaseValueGenerator
    {
        public UniformlyDistributedValue(float min, float max)
            : base(new ConstantValue(min), new ConstantValue(max))
        {
        }

        public UniformlyDistributedValue(IValueGenerator min, IValueGenerator max)
            : base(min, max)
        {
            Contract.Requires(min != null, "min != null");
            Contract.Requires(max != null, "max != null");
        }

        protected override float GenerateFloatValue(Func<double> random, INamedDataCollection data)
        {
            return random.RandomSingle(Min.SelectFloatValue(random, data), Max.SelectFloatValue(random, data));
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Min { get; [UsedImplicitly]set; }
            public float Max { get; [UsedImplicitly]set; }
            public bool Vary { get; [UsedImplicitly]set; }

            protected override IValueGenerator UnwrapImpl()
            {
                return new UniformlyDistributedValue(Min, Max).Transform(vary: Vary);
            }
        }
    }
}
