using Base_CityGeneration.Elements.Building.Internals.Rooms;
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
        : ISubdivisionContext
    {
        /// <summary>
        /// The wall section which this facade is filling in
        /// </summary>
        Walls.Section Section { get; set; }

        /// <summary>
        /// One of the two rooms neighbouring this wall (possibly null, if this is an external wall)
        /// </summary>
        IRoom Room1 { get; set; }

        /// <summary>
        /// One of the two rooms neighbouring this wall (possibly null, if this is an external wall)
        /// </summary>
        IRoom Room2 { get; set; }
    }

    public static class IFacadeExtensions
    {
        /// <summary>
        /// Given a facade and a room, select the room on the other side of the wall (or null if no such room exists)
        /// </summary>
        /// <param name="facade"></param>
        /// <param name="self"></param>
        /// <returns></returns>
        public static IRoom Partner(this IFacade facade, IRoom self)
        {
            if (facade.Room1 == self)
                return facade.Room2;
            else if (facade.Room2 == self)
                return facade.Room1;
            else
                return null;
        }
    }
}
