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
            var vertexMap = CreateVerticesFromGraph(mesh, seedVertex, position);

            //Add edges between vertices
            CreateEdgesFromGraph(mesh, autoPair, vertexMap);

            //Now we need to insert faces between the existing edges
            return CreateImplicitFaces(mesh);
        }

        private static void CreateEdgesFromGraph<TVertexTag, THalfEdgeTag, TFaceTag, TVertex>(Mesh<TVertexTag, THalfEdgeTag, TFaceTag> mesh, bool autoPair, Dictionary<TVertex, Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> vertexMap) where TVertex : IVertex
        {
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
        }

        private static Dictionary<TVertex, Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> CreateVerticesFromGraph<TVertexTag, THalfEdgeTag, TFaceTag, TVertex>(Mesh<TVertexTag, THalfEdgeTag, TFaceTag> mesh, TVertex seedVertex, Func<TVertex, Vector2> position) where TVertex : IVertex
        {
            var vertexMap = new Dictionary<TVertex, Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>();
            var verticesToProcess = new List<TVertex>() {
                seedVertex
            };
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
            return vertexMap;
        }

        /// <summary>
        /// Find areas of space which are surrounded by half edges and create a face in this space
        /// </summary>
        /// <typeparam name="TVertexTag"></typeparam>
        /// <typeparam name="THalfEdgeTag"></typeparam>
        /// <typeparam name="TFaceTag"></typeparam>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static Mesh<TVertexTag, THalfEdgeTag, TFaceTag> CreateImplicitFaces<TVertexTag, THalfEdgeTag, TFaceTag>(this Mesh<TVertexTag, THalfEdgeTag, TFaceTag> mesh)
        {
            //Build a list of edges which do not have a face attached
            var todo = new List<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>(mesh.HalfEdges.Where(a => a.Face == null));

            while (todo.Count > 0)
            {
                var edge = todo[todo.Count - 1];
                todo.RemoveAt(todo.Count - 1);

                //Skip this edge if it has had a face attached to it
                if (edge.Face != null)
                    continue;

                var path = TryWalkConnectedPath(edge);

                if (path == null)
                    continue;
                else
                {
                   
                }
            }


            throw new NotImplementedException();
        }

        private static LinkedList<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> TryWalkConnectedPath<TVertexTag, THalfEdgeTag, TFaceTag>(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edge)
        {
            //We have a half edge with no face attached...
            //...let's see if we can walk a path which comes back to this edge in which case we have found an enclosed space and can build a face
            var path = new LinkedList<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>();
            path.AddLast(edge);

            //Walk along path, always selecting the next half edge with the tightest turn
            var current = edge;
            var dir = Vector2.Normalize(current.EndVertex.Position - current.Pair.EndVertex.Position);
            do
            {
                //Find the edge which makes the tightest turn
                var smallestDot = float.PositiveInfinity;
                HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> bestEdge = null;
                var bestEdgeDir = Vector2.Zero;
                foreach (var e in current.EndVertex.Edges)
                {
                    //don't try to walk back along your own pair!
                    if (e.Pair.Equals(edge))
                        continue;

                    var eDir = Vector2.Normalize(e.EndVertex.Position - e.Pair.EndVertex.Position);
                    var dot = Vector2.Dot(dir, eDir);
                    if (dot < smallestDot)
                    {
                        smallestDot = dot;
                        bestEdge = e;
                        bestEdgeDir = eDir;
                    }
                }

                //Failed to find a next edge (dead end vertex)
                if (bestEdge == null)
                    break;

                //If we stepped from an edge not linked to a face to an edge linked to a face something is seriously wrong with the topology!
                if (bestEdge.Face != null)
                    throw new InvalidOperationException("Walking edges found an edge already connected to a face");

                //Save stuff for the next iteration
                current = bestEdge;
                path.AddLast(bestEdge);
                dir = bestEdgeDir;
            } while (path.Last.Value.Equals(path.First.Value));

            //Check for failure conditions
            if (path.Count < 3 || !path.Last.Value.Equals(path.First.Value))
                return null;

            return path;
        }
    }
}
