using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Eigens;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors
{
    public interface ITensorField
    {
        void Sample(ref Vector2 position, out Tensor result);
    }

    internal interface ITensorFieldContainer
    {
        ITensorField Unwrap();
    }

    public static class ITensorFieldExtensions
    {
        public static IEigenField Presample(this ITensorField field, Vector2 min, Vector2 max, int resolution)
        {
            return Eigens.ResampleAndRescale.Create(field, min, max, resolution);
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
