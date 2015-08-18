
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// An external facade of a building
    /// </summary>
    public interface IBuildingFacade
        : IFacade, ISubdivisionContext
    {
        /// <summary>
        /// Index of the lowest floor this facade covers
        /// </summary>
        int BottomFloorIndex { get; }

        /// <summary>
        /// Index of the highest floor this facade covers
        /// </summary>
        int TopFloorIndex { get; }
    }
}
