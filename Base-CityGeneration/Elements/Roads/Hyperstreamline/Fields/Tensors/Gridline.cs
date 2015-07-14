using System;
using Base_CityGeneration.Utilities.Numbers;
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

        internal class Container
            : ITensorFieldContainer
        {
            public float Angle { get; set; }
            public float? Length { get; set; }

            public ITensorField Unwrap()
            {
                return new Gridline(MathHelper.ToRadians(Angle), Length ?? 1);
            }
        }
    }
}
