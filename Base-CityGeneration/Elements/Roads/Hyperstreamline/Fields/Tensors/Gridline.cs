using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public class Gridline
        : ITensorField
    {
        private readonly Tensor _basis;

        public Gridline(float angle, float length)
        {
            _basis = Tensor.FromRTheta(length, angle);
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            result = _basis;
        }
    }
}
