using System;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class WrapperBaseValue
        : IValueGenerator
    {
        private readonly IValueGenerator _basis;
        private readonly Func<float, float> _transform;

        public WrapperBaseValue(IValueGenerator basis, Func<float, float> transform)
        {
            _basis = basis;
            _transform = transform;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            return _transform(_basis.SelectFloatValue(random, data));
        }

        public int SelectIntValue(Func<double> random, INamedDataCollection data)
        {
            return (int)Math.Round(_transform(_basis.SelectIntValue(random, data)));
        }
    }
}
