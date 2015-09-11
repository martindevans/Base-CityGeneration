
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Internals.VerticalFeatures
{
    /// <summary>
    /// A feature which is vertically rather than horizontally aligned. e.g. a stairwell which (obviously) cross multiple floors
    /// </summary>
    public interface IVerticalFeature
        : ISubdivisionContext
    {
        /// <summary>
        /// The bottom floor which this feature overlaps
        /// </summary>
        int BottomFloorIndex { get; set; }

        /// <summary>
        /// The top floor which this feature overlaps
        /// </summary>
        int TopFloorIndex { get; set; }
    }
}
