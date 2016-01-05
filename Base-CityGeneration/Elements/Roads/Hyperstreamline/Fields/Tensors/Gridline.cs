using System;
using Base_CityGeneration.Utilities.Numbers;
using System.Numerics;
using Myre.Collections;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

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
            public object Angle { get; set; }
            public float? Length { get; set; }

            public ITensorField Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                var angle = IValueGeneratorContainer.FromObject(Angle).SelectFloatValue(random, metadata);

                return new Gridline(MathHelper.ToRadians(angle), Length ?? 1);
            }
        }
    }
}
