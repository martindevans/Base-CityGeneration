using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IVertexBuilder
    {
        ReadOnlyCollection<Vector2> Shape { get; }
    }
}
