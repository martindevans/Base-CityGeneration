using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars
{
    public class Multiply
        : BaseScalarField
    {
        private readonly BaseScalarField _baseField;
        private readonly float _scale;

        public Multiply(BaseScalarField baseField, float scale)
        {
            _baseField = baseField;
            _scale = scale;
        }

        public override float Sample(Vector2 position)
        {
            return _baseField.Sample(position) * _scale;
        }
    }
}
