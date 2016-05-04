using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    /// <summary>
    /// Indicates that this room was formed from a floor plan
    /// </summary>
    public interface IPlannedRoom
        : IRoom
    {
        /// <summary>
        /// The room plan which caused this room to be created
        /// </summary>
        IRoomPlan Plan { get; set; }

        /// <summary>
        /// The facades which have been created for this room
        /// </summary>
        IReadOnlyDictionary<Facade, IConfigurableFacade> Facades { get; set; }
    }
}
