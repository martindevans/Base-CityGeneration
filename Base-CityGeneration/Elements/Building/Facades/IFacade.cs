using Base_CityGeneration.Geometry.Walls;

namespace Base_CityGeneration.Elements.Building.Facades
{
    /// <summary>
    /// A facade is a flat section of wall which is the boundary of some node. For example the facade of a floor is the outside of the building.
    /// </summary>
    public interface IFacade
    {
        /// <summary>
        /// The wall section which this facade is filling in
        /// </summary>
        Section Section { get; set; }
    }
}
