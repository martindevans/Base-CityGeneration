using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors
{
    public class Constant
        : IVector2Field
    {
        private readonly Vector2 _value;

        public Constant(Vector2 value)
        {
            _value = value;
        }

        public Vector2 Sample(Vector2 position)
        {
            return _value;
        }

        internal class Container
            : IVector2FieldContainer
        {
            public Vector2 Value { get; set; }

            public IVector2Field Unwrap()
            {
                return new Constant(Value);
            }
        }
    }
}
