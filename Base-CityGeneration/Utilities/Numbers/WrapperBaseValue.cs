using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class WrapperBaseValue
        : BaseValueGenerator
    {
        private readonly BaseValueGenerator _basis;
        private readonly Func<float, float> _transform;

        public WrapperBaseValue(BaseValueGenerator basis, Func<float, float> transform)
            : base(float.MinValue, float.MaxValue, true)
        {
            _basis = basis;
            _transform = transform;
        }

        protected override float GenerateFloatValue(Func<double> random)
        {
            return _transform(_basis.SelectFloatValue(random));
        }
    }
}
