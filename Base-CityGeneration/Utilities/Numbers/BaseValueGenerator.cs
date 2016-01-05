using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    [ContractClass(typeof(BaseValueGeneratorContracts))]
    public abstract class BaseValueGenerator
        : IValueGenerator
    {
        private readonly IValueGenerator _min;
        public IValueGenerator Min
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _min;
            }
        }

        private readonly IValueGenerator _max;
        public IValueGenerator Max
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _max;
            }
        }

        public float MaxValue
        {
            get { return _max.MaxValue; }
        }
        public float MinValue
        {
            get { return _min.MinValue; }
        }

        protected BaseValueGenerator(IValueGenerator min, IValueGenerator max)
        {
            Contract.Requires(min != null);
            Contract.Requires(max != null);

            _min = min;
            _max = max;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            return GenerateFloatValue(random, data);
        }

        protected abstract float GenerateFloatValue(Func<double> random, INamedDataCollection data);

        public static IValueGenerator Average(IValueGenerator a, IValueGenerator b)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            return new FuncValue(
                (r, m) => a.SelectFloatValue(r, m) * 0.5f + b.SelectFloatValue(r, m) * 0.5f,
                Math.Min(a.MinValue, b.MinValue),
                Math.Max(a.MinValue, a.MaxValue)
            );
        }
    }

    [ContractClassFor(typeof(BaseValueGenerator))]
    internal abstract class BaseValueGeneratorContracts
        : BaseValueGenerator
    {
        protected override float GenerateFloatValue(Func<double> random, INamedDataCollection data)
        {
            Contract.Requires(random != null);
            Contract.Requires(data != null);

            return default(float);
        }

        protected BaseValueGeneratorContracts(IValueGenerator min, IValueGenerator max)
            : base(min, max)
        {
        }
    }
}
