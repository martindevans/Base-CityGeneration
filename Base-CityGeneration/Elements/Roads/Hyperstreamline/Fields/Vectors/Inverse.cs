using System.Diagnostics.Contracts;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors
{
    internal class Inverse
        : IVector2Field
    {
        private readonly IVector2Field _field;

        public Inverse(IVector2Field field)
        {
            Contract.Requires(field != null);

            _field = field;
        }

        public Vector2 Sample(Vector2 position)
        {
            return -_field.Sample(position);
        }
    }
}
