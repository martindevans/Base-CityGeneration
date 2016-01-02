using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class WrapperValue
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

        private float? _singleCache;

        public WrapperValue(IValueGenerator basis, Func<float, float> transform, bool vary = true)
        {
            Contract.Requires(basis != null);
            Contract.Requires(transform != null);

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
    }
}
