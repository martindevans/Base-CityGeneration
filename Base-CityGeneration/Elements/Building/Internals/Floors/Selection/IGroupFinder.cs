using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection
{
    public interface IGroupFinder
    {
        HeightSpec Find(string group);
    }
}
