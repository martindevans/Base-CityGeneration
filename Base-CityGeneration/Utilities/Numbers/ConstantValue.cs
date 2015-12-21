using System;
using Myre.Collections;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class ConstantValue
        : IValueGenerator, IEquatable<ConstantValue>
    {
        private readonly float _value;

        public float MinValue
        {
            get { return _value; }
        }

        public float MaxValue
        {
            get { return _value; }
        }

        public ConstantValue(float value)
        {
            _value = value;
        }

        public float SelectFloatValue(Func<double> random, INamedDataCollection data)
        {
            return _value;
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

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantValue)
                return Equals(obj as ConstantValue);

            return ReferenceEquals(this, obj);
        }

        public bool Equals(ConstantValue other)
        {
            if (other == null)
                return false;

            return other._value.Equals(_value);
        }
    }
}
