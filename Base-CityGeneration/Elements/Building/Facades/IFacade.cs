using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Facades
{
    public interface IFacade
        : ISubdivisionContext
    {
        /// <summary>
        /// The wall section which this facade is filling in
        /// </summary>
        Walls.Section Section { get; set; }
    }
}
