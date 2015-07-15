using System;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Utilities.Numbers
{
    public abstract class BaseValueGenerator
    {
        private readonly float _min;
        public float Min
        {
            get
            {
                return _min;
            }
        }

        private readonly float _max;
        public float Max
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

        private int? _intCache;
        private float? _singleCache;

        protected BaseValueGenerator(float min, float max, bool vary)
        {
            _min = min;
            _max = max;
            _vary = vary;
        }

        public float SelectFloatValue(Func<double> random)
        {
            if (_singleCache.HasValue)
                return _singleCache.Value;

            var f = GenerateFloatValue(random);
            if (!Vary)
                _singleCache = f;
            return f;
        }

        protected abstract float GenerateFloatValue(Func<double> random); 

        public int SelectIntValue(Func<double> random)
        {
            if (_intCache.HasValue)
                return _intCache.Value;

            var i = GenerateIntValue(random);
            if (!Vary)
                _intCache = i;
            return i;
        }

        private int GenerateIntValue(Func<double> random)
        {
            //Rearrange the min and max to be integers (in a narrower or equal range)
            var min = (int)Math.Ceiling(Min);
            var max = (int)Math.Floor(Max);

            //If they're the same we don't have a whole lot of choice!
            if (min == max)
                return min;

            //If they're inverted the range is too narrow (e.g. Min:0.1 Max:0.9 we can't select any integers in that range)
            if (min > max)
                throw new InvalidOperationException(string.Format("Cannot select an integer between {0} and {1}", Min, Max));

            //Clamp and round the value
            return (int)Math.Round(MathHelper.Clamp(GenerateFloatValue(random), min, max), MidpointRounding.AwayFromZero);
        }
    }

    public static class IValueGeneratorExtensions
    {
        public static BaseValueGenerator Transform(this BaseValueGenerator gen, Func<float, float> func)
        {
            return new WrapperBaseValue(gen, func);
        }
    }

    internal abstract class BaseValueGeneratorContainer
    {
        BaseValueGenerator Unwrapped { get; set; }

        public BaseValueGenerator Unwrap()
        {
            if (Unwrapped == null)
                Unwrapped = UnwrapImpl();
            return Unwrapped;
        }

        protected abstract BaseValueGenerator UnwrapImpl();

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

        public static BaseValueGenerator FromObject(object v)
        {
            var @explicit = v as BaseValueGeneratorContainer;
            if (@explicit != null)
                return @explicit.Unwrap();

            float f = Convert.ToSingle(v);
            return ((BaseValueGeneratorContainer)f).Unwrap();
        }
    }
}
