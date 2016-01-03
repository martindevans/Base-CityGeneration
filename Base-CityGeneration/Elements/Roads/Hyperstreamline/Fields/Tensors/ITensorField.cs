using System;
using System.Diagnostics.Contracts;
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
        public static IEigenField Presample(this ITensorField field, Vector2 min, Vector2 max, uint resolution)
        {
            Contract.Requires(field != null);
            Contract.Ensures(Contract.Result<IEigenField>() != null);

            return ResampleAndRescale.Create(field, min, max, resolution);
        }

        public static Tensor Sample(this ITensorField field, Vector2 position)
        {
            Contract.Requires(field != null);

            Tensor result;
            field.Sample(ref position, out result);
            return result;
        }

        public static ITensorField DecayDistanceFromPoint(this ITensorField field, Vector2 center, float decay)
        {
            Contract.Requires(field != null);
            Contract.Ensures(Contract.Result<ITensorField>() != null);

            return new PointDistanceDecayField(field, center, decay);
        }
    }
}
