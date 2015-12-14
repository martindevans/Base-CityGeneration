using System;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class WrapperBaseValue
        : IValueGenerator
    {
        private readonly IValueGenerator _basis;
        private readonly Func<float, float> _transform;

        public float MaxValue
        {
            get { return _transform(_basis.MaxValue); }
        }
        public float MinValue
        {
            get { return _transform(_basis.MinValue); }
        }

        private readonly bool _vary;
        public bool Vary { get { return _vary; } }

        private int? _intCache;
        private float? _singleCache;

        public WrapperBaseValue(IValueGenerator basis, Func<float, float> transform, bool vary = true)
        {
            _basis = basis;
            _transform = transform;
            _vary = vary;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            if (_singleCache.HasValue && !_vary)
                return _singleCache.Value;

            _singleCache = _transform(_basis.SelectFloatValue(random, data));
            return _singleCache.Value;
        }

        public int SelectIntValue(Func<double> random, INamedDataCollection data)
        {
            if (_intCache.HasValue && !_vary)
                return _intCache.Value;

            _intCache = (int)Math.Round(_transform(_basis.SelectIntValue(random, data)));
            return _intCache.Value;
        }
    }
}
