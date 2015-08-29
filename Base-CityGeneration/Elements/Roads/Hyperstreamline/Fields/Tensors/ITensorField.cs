using System;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Eigens;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public interface ITensorField
    {
        void Sample(ref Vector2 position, out Tensor result);
    }

    internal interface ITensorFieldContainer
    {
        ITensorField Unwrap(Func<double> random, INamedDataCollection metadata);
    }

    public static class ITensorFieldExtensions
    {
        public static IEigenField Presample(this ITensorField field, Vector2 min, Vector2 max, int resolution)
        {
            return ResampleAndRescale.Create(field, min, max, resolution);
        }

        public static Tensor Sample(this ITensorField field, Vector2 position)
        {
            Tensor result;
            field.Sample(ref position, out result);
            return result;
        }

        public static ITensorField DecayDistanceFromPoint(this ITensorField field, Vector2 center, float decay)
        {
            return new PointDistanceDecayField(field, center, decay);
        }
    }
}
