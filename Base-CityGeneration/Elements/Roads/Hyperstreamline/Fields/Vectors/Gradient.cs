using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors
{
    internal class Gradient
        : IVector2Field
    {
        private readonly BaseScalarField _scalar;

        public Gradient(BaseScalarField scalar)
        {
            _scalar = scalar;
        }

        public Vector2 Sample(Vector2 position)
        {
            var v = _scalar.Sample(position);
            var x = _scalar.Sample(new Vector2(position.X + 1, position.Y));
            var y = _scalar.Sample(new Vector2(position.X, position.Y + 1));

            return new Vector2(
                v - x,
                v - y
            );
        }
    }
}
