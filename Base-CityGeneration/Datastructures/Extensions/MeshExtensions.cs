using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.HalfEdge;
using Placeholder.AI.Pathfinding.AStar;
using Placeholder.AI.Pathfinding.Graph;

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

        /// <summary>
        /// Starting from an arbitrary vertex in a graph, construct a half edge mesh representing the graph (may fail if graph is not topologically sound)
        /// </summary>
        /// <typeparam name="TVertexTag"></typeparam>
        /// <typeparam name="THalfEdgeTag"></typeparam>
        /// <typeparam name="TFaceTag"></typeparam>
        /// <typeparam name="TVertex"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="seedVertex"></param>
        /// <param name="position"></param>
        /// <param name="autoPair">If set then edges will automatically be made two way (if A->B exists then B->A will be created, even if it's not in the graph)</param>
        /// <returns></returns>
        public static Mesh<TVertexTag, THalfEdgeTag, TFaceTag> FromGraph<TVertexTag, THalfEdgeTag, TFaceTag, TVertex>(this Mesh<TVertexTag, THalfEdgeTag, TFaceTag> mesh, TVertex seedVertex, Func<TVertex, Vector2> position, bool autoPair = true)
            where TVertex : IVertex
        {
            Contract.Requires(mesh != null);
            Contract.Requires(seedVertex != null);

            //Create map from graph->mesh vertex
            var vertexMap = new Dictionary<TVertex, Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>();
            var verticesToProcess = new List<TVertex>() { seedVertex };
            while (verticesToProcess.Count > 0)
            {
                //Get a vertex to process
                var vertex = verticesToProcess[verticesToProcess.Count - 1];
                verticesToProcess.RemoveAt(verticesToProcess.Count - 1);

                //skip it if we're already done
                if (vertexMap.ContainsKey(vertex))
                    continue;

                //Construct a vertex and add it to the map
                vertexMap.Add(vertex, mesh.GetOrConstructVertex(position(vertex)));

                //Add connected vertices to vertices collection
                verticesToProcess.AddRange(vertex.OutwardEdges.Select(e => e.End).Cast<TVertex>());
            }

            //Add edges between vertices
            foreach (var kvp in vertexMap)
            {
                var vertex = kvp.Key;
                var start = kvp.Value;

                foreach (var edge in vertex.OutwardEdges)
                {
                    if (!edge.End.OutwardEdges.Any(e => e.End.Equals(vertex)) && !autoPair)
                        throw new ArgumentException("Input graph contains a vertex with a directed edge without a reverse edge (A->B exists but B->A does not)", "seedVertex");

                    //Map must contain this because we built map *from* connected edges
                    var end = vertexMap[(TVertex)edge.End];
                    mesh.GetOrConstructHalfEdge(start, end);
                }
            }

            throw new NotImplementedException();
        }
    }
}
