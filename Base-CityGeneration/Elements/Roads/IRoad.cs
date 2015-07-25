
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;

namespace Base_CityGeneration.Elements.Roads
{
    public interface IRoad
        : IGrounded
    {
        HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge { get; set; }
    }
}
