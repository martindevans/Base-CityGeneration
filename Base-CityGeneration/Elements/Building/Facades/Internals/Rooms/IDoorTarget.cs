
namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    public interface IDoorTarget
    {
        bool AllowConnectionTo(IPlannedRoom other);
    }
}
