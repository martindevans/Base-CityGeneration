using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars
{
    public abstract class BaseScalarField
    {
        public abstract float Sample(Vector2 position);

        public IVector2Field Gradient()
        {
            Contract.Ensures(Contract.Result<IVector2Field>() != null);

            return new Gradient(this);
        }

        public static BaseScalarField operator *(BaseScalarField field, float value)
        {
            Contract.Requires(field != null);
            Contract.Ensures(Contract.Result<BaseScalarField>() != null);

            return new Multiply(field, value);
        }
    }

    internal interface IScalarFieldContainer
        : IUnwrappable<BaseScalarField>
    {
    }

    internal static class BaseScalarFieldExtensions
    {
        public static float SafeSample(this BaseScalarField field, Vector2 position)
        {
            return field == null ? 0 : field.Sample(position);
        }
    }
}
