using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
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
            Contract.Requires(min != null, "min != null");
            Contract.Requires(max != null, "max != null");

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

            return new FuncValue(
                (r, m) => a.SelectFloatValue(r, m) * 0.5f + b.SelectFloatValue(r, m) * 0.5f,
                Math.Min(a.MinValue, b.MinValue),
                Math.Max(a.MinValue, a.MaxValue)
            );
        }
    }

    internal abstract class BaseValueGeneratorContainer
    {
        private IValueGenerator Unwrapped { get; set; }

        public IValueGenerator Unwrap()
        {
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            if (Unwrapped == null)
                Unwrapped = UnwrapImpl();
            return Unwrapped;
        }

        protected abstract IValueGenerator UnwrapImpl();

        public static explicit operator BaseValueGeneratorContainer(float v)
        {
            return new ConstantValue.Container { Value = v };
        }

        public static explicit operator BaseValueGeneratorContainer(double v)
        {
            return new ConstantValue.Container { Value = (float)v };
        }

        public static explicit operator BaseValueGeneratorContainer(int v)
        {
            return new ConstantValue.Container { Value = v };
        }

        public static IValueGenerator FromObject(object v, float? defaultValue = null)
        {
            Contract.Ensures(Contract.Result<IValueGenerator>() != null);

            var @explicit = v as BaseValueGeneratorContainer;
            if (@explicit != null)
                return @explicit.Unwrap();

            if (v == null)
            {
                if (defaultValue.HasValue)
                    v = defaultValue.Value;
                else
                    throw new ArgumentException("Value is null (and no default value was provided", "v");
            }

            var f = Convert.ToSingle(v);
            return ((BaseValueGeneratorContainer)f).Unwrap();
        }
    }
}
