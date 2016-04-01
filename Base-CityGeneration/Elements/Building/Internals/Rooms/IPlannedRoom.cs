//using System;
//using System.Collections.Generic;
//using System.Diagnostics.Contracts;
//using Base_CityGeneration.Elements.Building.Facades;
//using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;

//namespace Base_CityGeneration.Elements.Building.Internals.Rooms
//{
//    /// <summary>
//    /// Indicates that this room was formed from a floor plan
//    /// </summary>
//    public interface IPlannedRoom
//        : IRoom
//    {
//        IReadOnlyDictionary<IRoomFacade, IConfigurableFacade> Facades { set; }
//    }

//    public static class IPlannedRoomExtensions
//    {
//        public static IRoomPlan FindPlan(this IPlannedRoom room, params Type[] endStop)
//        {
//            Contract.Requires(room != null);
//            Contract.Requires(endStop != null);

//            return TreeSearch.SearchUp<IRoomPlan, IRoomPlanProvider>(room, a => a.GetPlan(room), endStop);
//        }
//    }
//}
