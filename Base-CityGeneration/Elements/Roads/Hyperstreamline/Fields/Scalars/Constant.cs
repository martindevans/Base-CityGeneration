using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars
{
    public class Constant
        : BaseScalarField
    {
        private readonly float _constant;

        public Constant(float constant)
        {
            _constant = constant;
        }

        public override float Sample(Vector2 position)
        {
            return _constant;
        }

        internal class Container
            : IScalarFieldContainer
        {
            public float Value { get; set; }

            public BaseScalarField Unwrap()
            {
                return new Constant(Value);
            }
        }
    }
}
