using System;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    internal class PointDistanceDecayField
        : ITensorField
    {
        private readonly ITensorField _field;
        private readonly Vector2 _center;
        private readonly float _decay;

        public PointDistanceDecayField(ITensorField field, Vector2 center, float decay)
        {
            _field = field;
            _center = center;
            _decay = decay;
        }

        public void Sample(ref Vector2 position, out Tensor result)
        {
            var exp = DistanceDecay(_decay, (position - _center).LengthSquared());

            Tensor sample;
            _field.Sample(ref position, out sample);

            result = exp * sample;
        }

        internal static float DistanceDecay(float distanceSqr, float decay)
        {
            return (float)Math.Exp(-decay * distanceSqr);
        }
    }
}
