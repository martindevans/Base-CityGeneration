using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using HandyCollections.Set;
using Placeholder.AI.Pathfinding.AStar;
using SquarifiedTreemap.Extensions;
using SwizzleMyVectors;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class MeshExtensions
    {
        #region pathfind
        // ReSharper disable UnusedParameter.Global
        public static IEnumerable<HalfEdge<TVTag, THTag, TFTag>> Pathfind<TVTag, THTag, TFTag>(
            this Mesh<TVTag, THTag, TFTag> m,
// ReSharper restore UnusedParameter.Global
            Vertex<TVTag, THTag, TFTag> start,
            Vertex<TVTag, THTag, TFTag> end
        )
        {
            Contract.Requires(m != null);
            Contract.Requires(start != null);
            Contract.Requires(end != null);
            Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TVTag, THTag, TFTag>>>() != null);

            if (start.Mesh != m)
                throw new ArgumentException("Start vertex is not contained in mesh for pathfind", "start");
            if (end.Mesh != m)
                throw new ArgumentException("End vertex is not contained in mesh for pathfind", "end");

            var p = Pathfinder.Get();
            try
            {
                // ReSharper disable once HeapView.SlowDelegateCreation
                var edges = p.FindPath(start, end, (a, b) => (((Vertex<TVTag, THTag, TFTag>)a).Position - ((Vertex<TVTag, THTag, TFTag>)b).Position).Length());

                return edges.Cast<HalfEdge<TVTag, THTag, TFTag>>();
            }
            finally
            {
                Pathfinder.Return(p);
            }
        }
        #endregion

        #region implicit faces
        /// <summary>
        /// Find areas of space which are surrounded by half edges and create a face in this space
        /// </summary>
        /// <typeparam name="TVTag"></typeparam>
        /// <typeparam name="TETag"></typeparam>
        /// <typeparam name="TFTag"></typeparam>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static void CreateImplicitFaces<TVTag, TETag, TFTag>(this Mesh<TVTag, TETag, TFTag> mesh)
        {
            //Build a list of edges which do not have a face attached
            var todo = new List<HalfEdge<TVTag, TETag, TFTag>>(mesh.HalfEdges.Where(a => a.Face == null));

            while (todo.Count > 0)
            {
                //Remove *last* face (more efficient than removing first)
                var edge = todo[todo.Count - 1];
                todo.RemoveAt(todo.Count - 1);

                //Skip this edge if it has had a face attached to it
                if (edge.Face != null)
                    continue;

                //From this edge walk a path of edges (always turning as tight as possible to the right)
                var path = TryWalkClosedClockwisePath(edge);
                if (path != null)
                {
                    if (path.Select(a => a.EndVertex.Position).IsClockwise())
                        mesh.GetOrConstructFace(path.Select(a => a.EndVertex).ToArray());
                }
            }
        }

        private static IEnumerable<HalfEdge<TVTag, THTag, TFTag>> TryWalkClosedClockwisePath<TVTag, THTag, TFTag>(HalfEdge<TVTag, THTag, TFTag> edge)
        {
            //Path we have walked
            var path = new OrderedSet<HalfEdge<TVTag, THTag, TFTag>> { edge };

            var current = edge;
            do
            {

                current = SelectTightestClockwiseTurn(current);

                //Check for failing to find an edge
                if (current == null)
                    break;

                if (!path.Add(current))
                {
                    //We failed to add this edge
                    //if this is the first edge that's great we've found a closed path
                    //if not, then it's an invalid path todo: does this imply topology is broken?
                    if (current.Equals(path.First()))
                        break;
                    else
                        return null;
                }
            } while (true);

            //Check for invalid face
            if (path.Count < 3)
                return null;

            //Yay, it's valid
            return path;
        }

        private static HalfEdge<TVTag, THTag, TFTag> SelectTightestClockwiseTurn<TVTag, THTag, TFTag>(HalfEdge<TVTag, THTag, TFTag> edge)
        {
            var dir = Vector2.Normalize(edge.EndVertex.Position - edge.StartVertex.Position);

            //Find the edge which makes the *largest* turn:
            //  180 degrees is a complete reversal, very large and very tight
            //  0 degrees is straight on, not very tight
            //  -179 is a complete reversal on the otherside, loosest possible turn
            var bestAngle = float.NegativeInfinity;
            HalfEdge<TVTag, THTag, TFTag> bestEdge = null;
            foreach (var e in edge.EndVertex.Edges)
            {
                //don't try to walk back along your own pair!
                if (e.Pair.Equals(edge))
                    continue;

                var eDir = Vector2.Normalize(e.EndVertex.Position - e.Pair.EndVertex.Position);

                //Calculate clockwise turn angle (from -pi to +pi)
                var dot = Vector2.Dot(dir, eDir);
                var det = dir.Cross(eDir);
                var angle = -(float)Math.Atan2(det, dot);

                //Keep track of the best edge so far
                if (angle > bestAngle)
                {
                    bestAngle = angle;
                    bestEdge = e;
                }
            }

            return bestEdge;
        }
        #endregion

        #region simplify
        /// <summary>
        /// Find pairs of edges along the boundary of two faces which are completely linear (i.e. the middle vertex is useless) and remove middle vertex
        /// </summary>
        /// <typeparam name="TVTag"></typeparam>
        /// <typeparam name="THTag"></typeparam>
        /// <typeparam name="TFTag"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="angleThreshold"></param>
        public static void SimplifyFaces<TVTag, THTag, TFTag>(this Mesh<TVTag, THTag, TFTag> mesh, float angleThreshold = 0.00872665f)
        {
            Contract.Requires(mesh != null);

            float threshold = 1 - (float)Math.Cos(angleThreshold);

            //Build a set of all faces to process
            var todo = new List<Face<TVTag, THTag, TFTag>>(mesh.Faces);

            while (todo.Count > 0)
            {
                //Remove the last item (more efficient than the first)
                var face = todo[todo.Count - 1];
                todo.RemoveAt(todo.Count - 1);

                //Faces could be deleted before we get to them
                if (face.IsDeleted)
                    continue;

                var edges = face.Edges.ToArray();
                for (var i = 0; i < edges.Length; i++)
                {
                    var ab = edges[i];
                    var bc = edges[(i + 1) % edges.Length];

                    //We can only consider removing this vertex if the faces on both sides are the same!
                    if (ab.Pair.Face == null || bc.Pair.Face == null || !ab.Pair.Face.Equals(bc.Pair.Face))
                        continue;

                    var abDir = Vector2.Normalize(ab.EndVertex.Position - ab.StartVertex.Position);
                    var bcDir = Vector2.Normalize(bc.EndVertex.Position - bc.StartVertex.Position);

                    //Check if these two lines point in the same direction, if so we can collapse them into one edge
                    if (Vector2.Dot(abDir, bcDir).TolerantEquals(1, threshold))
                    {
                        //Get lists of vertices in both faces (except the vertex we're removing)
                        var f1 = ab.Face;
                        var f1Vertices = f1.Vertices.Where(v => !v.Equals(ab.EndVertex)).ToArray();

                        var f2 = ab.Pair.Face;
                        var f2Vertices = f2.Vertices.Where(v => !v.Equals(ab.EndVertex)).ToArray();

                        //Delete the faces
                        mesh.Delete(f1);
                        mesh.Delete(f2);

                        //Delete the vertex
                        mesh.Delete(ab.EndVertex);

                        //Create 2 new faces (copy across tags)
                        var fn1 = mesh.GetOrConstructFace(f1Vertices);
                        fn1.Tag = f1.Tag;
                        var fn2 = mesh.GetOrConstructFace(f2Vertices);
                        fn2.Tag = f2.Tag;

                        //Add these two new faces to the queue
                        todo.Add(fn1);
                        todo.Add(fn2);
                        break;
                    }
                }
            }
        }
        #endregion
    }
}
