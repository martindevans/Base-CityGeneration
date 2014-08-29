using System;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    /// <summary>
    /// Indicates that this room was formed from a floor plan
    /// </summary>
    public interface IPlannedRoom
        : IRoom
    {
    }

    public static class IPlannedRoomExtensions
    {
        public static RoomPlan FindPlan(this IPlannedRoom room, params Type[] endStop)
        {
            return TreeSearch.SearchUp<RoomPlan, IRoomPlanProvider>(room, a => a.GetPlan(room), endStop);
        }
    }
}
