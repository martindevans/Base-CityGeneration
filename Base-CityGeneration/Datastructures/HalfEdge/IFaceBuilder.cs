using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IFaceBuilder
    {
        Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Face { get; }

        ReadOnlyCollection<Vector2> Shape { get; }
    }
}
