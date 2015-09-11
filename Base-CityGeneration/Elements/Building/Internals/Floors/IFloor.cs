using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public interface IFloor
        : ISubdivisionContext
    {
        int FloorIndex { get; set; }

        IReadOnlyCollection<IVerticalFeature> Overlaps { set; }

        /// <summary>
        /// Choose the footprint for a given vertical feature
        /// </summary>
        /// <param name="feature">The vertical feature we are placing (which *starts* at this floor)</param>
        /// <param name="space">The available space to place the vertical feature within (may be restricted by higher floors being a different shape)</param>
        /// <param name="floors">The floors which this vertical feature crosses</param>
        /// <returns>The footprint of this feature</returns>
        IEnumerable<Vector2> PlaceVerticalFeature(VerticalSelection feature, IReadOnlyList<Vector2> space, IReadOnlyList<IFloor> floors);
    }
}
