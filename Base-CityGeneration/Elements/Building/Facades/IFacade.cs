using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// A facade is a flat section of wall which is the boundary of some node. For example the facade of a floor is the outside of the building.
    /// 
    /// Facades are difficult because they are owned by the floor (in this example) but are really part of the building. IFacadeOwner (the floor) and IFacadeProvider (the building) solve this problem
    /// </summary>
    public interface IFacade
    {
        /// <summary>
        /// The wall section which this facade is filling in
        /// </summary>
        Walls.Section Section { get; set; }
    }
}
