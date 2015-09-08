using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public interface IFloor
        : ISubdivisionContext
    {
        int FloorIndex { get; set; }

        IReadOnlyCollection<IVerticalFeature> Overlaps { set; }
    }
}
