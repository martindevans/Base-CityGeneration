
using System.Collections.Generic;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Ref
{
    public interface IRef
    {
        VerticalElementCreationOptions Filter { get; }

        IEnumerable<FloorSelection> Match(int basements, FloorSelection[] floors, int? startIndex = null);
    }

    internal interface IRefContainer
    {
        IRef Unwrap();
    }
}
