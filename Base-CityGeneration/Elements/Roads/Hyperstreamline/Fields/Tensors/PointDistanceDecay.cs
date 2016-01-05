using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using System.Numerics;
using JetBrains.Annotations;
using Myre.Collections;

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
            Contract.Requires(field != null);

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

        internal class Container
            : ITensorFieldContainer
        {
            public ITensorFieldContainer Tensors { get; [UsedImplicitly]set; }
            public Vector2Container Center { get; [UsedImplicitly]set; }
            public object Decay { get; [UsedImplicitly]set; }

            public ITensorField Unwrap(Func<double> random, INamedDataCollection metadata)
            {
                Contract.Assume(Tensors != null);
                Contract.Assume(Center != null);

                return new PointDistanceDecayField(
                    Tensors.Unwrap(random, metadata),
                    Center.Unwrap(random, metadata),
                    IValueGeneratorContainer.FromObject(Decay).SelectFloatValue(random, metadata)
                );
            }
        }
    }
}
