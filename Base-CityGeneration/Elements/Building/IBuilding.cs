using System.Collections.Generic;
using Base_CityGeneration.Elements.Blocks;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Generic;

namespace Base_CityGeneration.Elements.Building
{
    public interface IBuilding
        : IGrounded, INeighbour
    {
        /// <summary>
        /// Total number of floors (above + below ground)
        /// </summary>
        int TotalFloors { get; }

        /// <summary>
        /// Get the number of floors above ground
        /// </summary>
        int AboveGroundFloors { get; }

        /// <summary>
        /// Get the number of floors below ground
        /// </summary>
        int BelowGroundFloors { get; }

        /// <summary>
        /// Get the floor at the given index (possibly negative, to reference basements)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IFloor Floor(int index);

        /// <summary>
        /// Get all vertical features which overlap the given range of floor
        /// </summary>
        /// <param name="lowest">The lowest floor in the range</param>
        /// <param name="highest">The highest floor in the range</param>
        /// <returns></returns>
        IEnumerable<IVerticalFeature> Verticals(int lowest, int highest);

        /// <summary>
        /// Get all the facades surrounding a particular floor
        /// </summary>
        /// <param name="floor"></param>
        /// <returns></returns>
        IEnumerable<IBuildingFacade> Facades(int floor);
    }
}
