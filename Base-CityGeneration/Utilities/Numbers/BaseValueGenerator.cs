using System;
using Myre.Collections;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

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
                return _min;
            }
        }

        private readonly IValueGenerator _max;
        public IValueGenerator Max
        {
            get
            {
                return _max;
            }
        }

        private readonly bool _vary;
        public bool Vary
        {
            get
            {
                return _vary;
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

        private int? _intCache;
        private float? _singleCache;

        protected BaseValueGenerator(IValueGenerator min, IValueGenerator max, bool vary)
        {
            _min = min;
            _max = max;
            _vary = vary;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            if (_singleCache.HasValue)
                return _singleCache.Value;

            var f = GenerateFloatValue(random, data);
            if (!Vary)
                _singleCache = f;
            return f;
        }

        protected abstract float GenerateFloatValue(Func<double> random, INamedDataCollection data);

        public int SelectIntValue(Func<double> random, INamedDataCollection data)
        {
            if (_intCache.HasValue)
                return _intCache.Value;

            var i = GenerateIntValue(random, data);
            if (!Vary)
                _intCache = i;
            return i;
        }

        private int GenerateIntValue(Func<double> random, INamedDataCollection data)
        {
            checked
            {
                //Rearrange the min and max to be integers (in a narrower or equal range)
                var min = (long)Math.Ceiling(Min.SelectFloatValue(random, data));
                var max = (long)Math.Floor(Max.SelectFloatValue(random, data));

                //If they're the same we don't have a whole lot of choice!
                if (min == max)
                    return (int)min;

                //If they're inverted the range is too narrow (e.g. Min:0.1 Max:0.9 we can't select any integers in that range)
                if (min > max)
                    throw new InvalidOperationException(string.Format("Cannot select an integer between {0} and {1}", Min, Max));

                //Clamp and round the value
                return (int)Math.Round(MathHelper.Clamp(GenerateFloatValue(random, data), min, max), MidpointRounding.AwayFromZero);
            }
        }
    }

    public static class IValueGeneratorExtensions
    {
        public static IValueGenerator Transform(this IValueGenerator gen, Func<float, float> func)
        {
            return new WrapperBaseValue(gen, func);
        }
    }

    internal abstract class BaseValueGeneratorContainer
    {
        IValueGenerator Unwrapped { get; set; }

        public IValueGenerator Unwrap()
        {
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

        public static IValueGenerator FromObject(object v)
        {
            var @explicit = v as BaseValueGeneratorContainer;
            if (@explicit != null)
                return @explicit.Unwrap();

            float f = Convert.ToSingle(v);
            return ((BaseValueGeneratorContainer)f).Unwrap();
        }
    }
}
