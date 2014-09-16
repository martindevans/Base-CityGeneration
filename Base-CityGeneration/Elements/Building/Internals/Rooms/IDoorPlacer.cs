using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    public interface IDoorPlacer
    {
        bool ConnectTo(RoomPlan otherRoom);
    }
}
