using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars
{
    public abstract class BaseScalarField
    {
        public abstract float Sample(Vector2 position);

        public IVector2Field Gradient()
        {
            return new Gradient(this);
        }

        public static BaseScalarField operator *(BaseScalarField field, float value)
        {
            return new Multiply(field, value);
        }
    }

    internal static class IScalarFieldExtensions
    {
        public static float SafeSample(this BaseScalarField field, Vector2 position)
        {
            return field == null ? 0 : field.Sample(position);
        }
    }
}
