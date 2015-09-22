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
        /// <summary>
        /// Index of this floor in parent building (ground floor is zero, basements are negative)
        /// </summary>
        int FloorIndex { get; set; }

        /// <summary>
        /// Altitude of the *base* of this floor
        /// </summary>
        float FloorAltitude { get; set; }

        /// <summary>
        /// Height of this floor
        /// </summary>
        float FloorHeight { get; set; }

        /// <summary>
        /// Vertical features which cross this floor
        /// </summary>
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
