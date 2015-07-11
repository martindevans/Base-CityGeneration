using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Placeholder.AI.Pathfinding.AStar;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class MeshExtensions
    {
// ReSharper disable UnusedParameter.Global
        public static IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> Pathfind<TVertexTag, THalfEdgeTag, TFaceTag>(
            this Mesh<TVertexTag, THalfEdgeTag, TFaceTag> m,
// ReSharper restore UnusedParameter.Global
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start,
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end
        )
        {
            if (start.Mesh != m)
                throw new ArgumentException("Start vertex is not contained in mesh for pathfind", "start");
            if (end.Mesh != m)
                throw new ArgumentException("End vertex is not contained in mesh for pathfind", "end");

            Pathfinder p = Pathfinder.Get();
            try
            {
                // ReSharper disable once HeapView.SlowDelegateCreation
                var edges = p.FindPath(start, end, (a, b) => (((Vertex<TVertexTag, THalfEdgeTag, TFaceTag>)a).Position - ((Vertex<TVertexTag, THalfEdgeTag, TFaceTag>)b).Position).Length());

                return edges.Cast<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>();
            }
            finally
            {
                Pathfinder.Return(p);
            }
        }
    }
}
