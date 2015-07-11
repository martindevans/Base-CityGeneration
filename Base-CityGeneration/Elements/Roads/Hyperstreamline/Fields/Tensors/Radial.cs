using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Radial
        : ITensorField
    {
        private readonly Vector2 _center;

        public Radial(Vector2 center)
        {
            _center = center;
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = Tensor.FromXY(position - _center);
        }
    }
}
