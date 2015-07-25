
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;

namespace Base_CityGeneration.Elements.Roads
{
    public interface IJunction
        : IGrounded
    {
        Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Vertex { get; set; }
    }
}
