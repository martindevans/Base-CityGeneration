using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Eigens
{
    public interface IEigenField
    {
        IVector2Field MajorEigenVectors { get; }

        IVector2Field MinorEigenVectors { get; }
    }
}
