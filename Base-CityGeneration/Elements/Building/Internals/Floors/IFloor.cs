using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public interface IFloor
        : ISubdivisionContext
    {
        BaseBuilding ParentBuilding { get; set; }

        int FloorIndex { get; set; }

        IVerticalFeature[] Overlaps { get; set; }
    }
}
