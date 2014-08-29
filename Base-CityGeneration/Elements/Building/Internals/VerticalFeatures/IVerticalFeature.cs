
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;

namespace Base_CityGeneration.Elements.Building.Internals.VerticalFeatures
{
    /// <summary>
    /// A feature which is vertically rather than horizontally aligned. e.g. a stairwell which (obviously) cross multiple floors
    /// </summary>
    public interface IVerticalFeature
        : ISubdivisionContext
    {
        /// <summary>
        /// Indicates if this is a major vertical feature (e.g. stairwell or lift) which you may want to lead a corridor up to or a minor vertical feature (e.g. utility shaft) which you probably don't!
        /// </summary>
        bool IsMajorFeature { get; }

        /// <summary>
        /// The bottom floor which this feature overlaps
        /// </summary>
        int BottomFloorIndex { get; set; }

        /// <summary>
        /// The top floor which this feature overlaps
        /// </summary>
        int TopFloorIndex { get; set; }

        /// <summary>
        /// The height of each floor
        /// </summary>
        float FloorHeight { get; set; }
    }
}
