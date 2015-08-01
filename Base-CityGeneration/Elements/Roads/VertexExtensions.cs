using Base_CityGeneration.Datastructures.HalfEdge;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Base_CityGeneration.Elements.Roads
{
    internal static class VertexExtensions
    {
        public static IEnumerable<IHalfEdgeBuilder> OrderedEdges(this Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> vertex)
        {
            //Order the edges by their angle around the vertex
            return (from edge in vertex.Edges
                    let b = edge.BuilderEndingWith(vertex)
                    let angle = (float)Math.Atan2(b.Direction.Y, b.Direction.X)
                    orderby angle descending
                    select b);
        }
    }
}
