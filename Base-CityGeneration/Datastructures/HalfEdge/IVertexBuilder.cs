using System.Collections.ObjectModel;
using System.Numerics;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IVertexBuilder
    {
        ReadOnlyCollection<Vector2> Shape { get; }
    }
}
