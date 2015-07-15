using System;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class ConstantValue
        : BaseValueGenerator
    {
        private readonly float _value;

        public ConstantValue(float value)
            : base(float.MinValue, float.MaxValue, false)
        {
            _value = value;
        }

        protected override float GenerateFloatValue(Func<double> random)
        {
            return _value;
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Value { get; set; }

            protected override BaseValueGenerator UnwrapImpl()
            {
                return new ConstantValue(Value);
            }
        }
    }
}
