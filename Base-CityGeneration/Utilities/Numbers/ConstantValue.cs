using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class ConstantValue
        : IValueGenerator
    {
        private readonly float _value;

        public ConstantValue(float value)
        {
            _value = value;
        }

        public float SelectFloatValue(Func<double> random)
        {
            return _value;
        }

        public int SelectIntValue(Func<double> random)
        {
            return (int)_value;
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Value { get; set; }

            protected override IValueGenerator UnwrapImpl()
            {
                return new ConstantValue(Value);
            }
        }
    }
}
