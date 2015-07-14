using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public interface IValueGenerator
    {
        float SelectFloatValue(Func<double> random);

        int SelectIntValue(Func<double> random);
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

        public static BaseValueGeneratorContainer FromObject(object v)
        {
            var @explicit = v as BaseValueGeneratorContainer;
            if (@explicit != null)
                return @explicit;

            float f = Convert.ToSingle(v);
            return (BaseValueGeneratorContainer)f;
        }
    }
}
