using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public interface IFaceBuilder
    {
        Face<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Face { get; }

        Vector2[] Shape { get; }
    }
}
