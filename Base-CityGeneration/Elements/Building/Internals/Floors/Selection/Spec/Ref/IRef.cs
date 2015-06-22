
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public interface IRef
    {
        IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors);
    }

    internal interface IRefContainer
    {
        IRef Unwrap();
    }
}
