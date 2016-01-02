using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            Contract.Requires(m != null);
            Contract.Requires(start != null);
            Contract.Requires(end != null);
            Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);

            if (start.Mesh != m)
                throw new ArgumentException("Start vertex is not contained in mesh for pathfind", "start");
            if (end.Mesh != m)
                throw new ArgumentException("End vertex is not contained in mesh for pathfind", "end");

            var p = Pathfinder.Get();
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
