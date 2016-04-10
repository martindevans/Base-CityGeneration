using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using HandyCollections.Set;
using Placeholder.AI.Pathfinding.AStar;
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

            var p = Pathfinder<Vertex<TVTag, THTag, TFTag>, HalfEdge<TVTag, THTag, TFTag>>.Get();
            Contract.Assume(p != null);
            try
            {
                // ReSharper disable once HeapView.SlowDelegateCreation
                var edges = p.FindPath(start, end, (a, b) => (a.Position - b.Position).Length());

                return edges;
            }
            finally
            {
                Pathfinder<Vertex<TVTag, THTag, TFTag>, HalfEdge<TVTag, THTag, TFTag>>.Return(p);
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
        /// <param name="createTag"></param>
        /// <returns></returns>
        public static void CreateImplicitFaces<TVTag, TETag, TFTag>(this Mesh<TVTag, TETag, TFTag> mesh, Func<Face<TVTag, TETag, TFTag>, TFTag> createTag = null)
        {
            Contract.Requires(mesh != null);

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
                    {
                        var face = mesh.GetOrConstructFace(path.Select(a => a.EndVertex).ToArray());

                        if (createTag != null && face.Tag == null)
                            face.Tag = createTag(face);
                    }
                }
            }
        }

        private static IEnumerable<HalfEdge<TVTag, THTag, TFTag>> TryWalkClosedClockwisePath<TVTag, THTag, TFTag>(HalfEdge<TVTag, THTag, TFTag> edge)
        {
            Contract.Requires(edge != null);

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
            Contract.Requires(edge != null);

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

        #region cleaning
        public static void RemoveDisconnectedEdges<TVTag, THTag, TFTag>(this Mesh<TVTag, THTag, TFTag> mesh)
        {
            Contract.Requires(mesh != null);

            foreach (var edge in mesh.HalfEdges.Where(a => a.IsPrimaryEdge && a.Face == null && a.Pair.Face == null).ToArray())
                mesh.Delete(edge);
        }

        public static void RemoveDisconnectedVertices<TVTag, THTag, TFTag>(this Mesh<TVTag, THTag, TFTag> mesh)
        {
            Contract.Requires(mesh != null);

            foreach (var vertex in mesh.Vertices.Where(a => a.EdgeCount == 0).ToArray())
                mesh.Delete(vertex);
            
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
        public static void SimplifyFaces<TVTag, THTag, TFTag>(this Mesh<TVTag, THTag, TFTag> mesh, float angleThreshold = 0.015f)
        {
            Contract.Requires(mesh != null);

            var threshold = 1 - (float)Math.Cos(angleThreshold);

            Func<IEnumerable<Vertex<TVTag, THTag, TFTag>>, bool> simplify = vertices => {

                var remove = vertices.FirstOrDefault(v => {

                    //Must be on a line, that means two attached edges
                    if (v.EdgeCount != 2)
                        return false;

                    //Must be between two faces (or nulls)
                    var av = v.Edges.Skip(1).First().Pair;
                    var vb = v.Edges.First();

                    //Check if in and out edges do not border same face
                    if (!ReferenceEquals(av.Face, vb.Face))
                        return false;

                    //Same check for the other side
                    if (!ReferenceEquals(av.Pair.Face, vb.Pair.Face))
                        return false;

                    //Must be on a *straight* line
                    var avd = Vector2.Normalize(av.EndVertex.Position - av.StartVertex.Position);
                    var vbd = Vector2.Normalize(vb.EndVertex.Position - vb.StartVertex.Position);
                    if (!Vector2.Dot(avd, vbd).TolerantEquals(1, threshold))
                        return false;

                    return true;
                });

                if (remove == null)
                    return false;

                var ab = remove.Edges.Skip(1).First().Pair;

                //Get lists of vertices in both faces (except the vertex we're removing)
                var f1 = ab.Face;
                var f1Vertices = f1 == null ? null :f1.Vertices.Where(v => !v.Equals(ab.EndVertex)).ToArray();

                var f2 = ab.Pair.Face;
                var f2Vertices = f2 == null ? null : f2.Vertices.Where(v => !v.Equals(ab.EndVertex)).ToArray();

                //Delete the faces
                if (f1 != null)
                    mesh.Delete(f1);
                if (f2 != null)
                    mesh.Delete(f2);

                //Delete the vertex
                mesh.Delete(ab.EndVertex);

                //Create 2 new faces (copy across tags)
                if (f1 != null)
                {
                    var fn1 = mesh.GetOrConstructFace(f1Vertices);
                    var t1 = f1.Tag;
                    f1.Tag = default(TFTag);
                    fn1.Tag = t1;
                }

                if (f2 != null)
                {
                    var fn2 = mesh.GetOrConstructFace(f2Vertices);
                    var t2 = f2.Tag;
                    f2.Tag = default(TFTag);
                    fn2.Tag = t2;
                }

                return true;
            };

            simplify.Fixpoint(mesh.Vertices);
        }
        #endregion

        #region Face extensions
        public static float Area<TV, TE, TF>(this Face<TV, TE, TF> face)
        {
            Contract.Requires(face != null);

            return face
                .Vertices
                .Select(a => a.Position)
                .Area();
        }
        #endregion

        #region delete enumerables
        public static void Delete<TV, TE, TF>(this Mesh<TV, TE, TF> mesh, IEnumerable<Face<TV, TE, TF>> faces)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(faces != null);

            foreach (var face in faces)
                if (!face.IsDeleted)
                    mesh.Delete(face);
        }

        public static void Delete<TV, TE, TF>(this Mesh<TV, TE, TF> mesh, IEnumerable<HalfEdge<TV, TE, TF>> edges)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(edges != null);

            foreach (var edge in edges)
                if (!edge.IsDeleted)
                    mesh.Delete(edge);
        }

        public static void Delete<TV, TE, TF>(this Mesh<TV, TE, TF> mesh, IEnumerable<Vertex<TV, TE, TF>> vertices)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(vertices != null);

            foreach (var vertex in vertices)
                if (!vertex.IsDeleted)
                    mesh.Delete(vertex);
        }
        #endregion

        #region tags
        /// <summary>
        /// Get the tags of the edges leading *away* from this vertex, ordered by angle (starting with an arbitrary vertex)
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <typeparam name="TF"></typeparam>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public static IEnumerable<TE> OrderedEdgeTags<TV, TE, TF>(this Vertex<TV, TE, TF> vertex)
        {
            Contract.Requires(vertex != null);
            Contract.Ensures(Contract.Result<IEnumerable<TE>>() != null);

            //Order the edges by their angle around the vertex
            return (from edge in vertex.Edges
                    let tag = edge.Pair.Tag
                    let direction = edge.Pair.Segment.Line.Direction
                    let angle = (float)Math.Atan2(direction.Y, direction.X)
                    orderby angle descending
                    select tag);
        }

        /// <summary>
        /// Convert each tag to a new tag, attach the new tag *without* calling detach on the old tag
        /// </summary>
        /// <typeparam name="TVTag"></typeparam>
        /// <typeparam name="THTag"></typeparam>
        /// <typeparam name="TFTag"></typeparam>
        /// <param name="mesh"></param>
        /// <param name="wrapV"></param>
        /// <param name="wrapE"></param>
        /// <param name="wrapF"></param>
        /// <param name="wrap">if true then attach will be called, detach will not. if false, the reverse</param>
        internal static void WrapTags<TVTag, THTag, TFTag>(
            this Mesh<TVTag, THTag, TFTag> mesh,
            Func<Vertex<TVTag, THTag, TFTag>, TVTag> wrapV,
            Func<HalfEdge<TVTag, THTag, TFTag>, THTag> wrapE,
            Func<Face<TVTag, THTag, TFTag>, TFTag> wrapF,
            bool wrap)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(wrapV != null);
            Contract.Requires(wrapE != null);
            Contract.Requires(wrapF != null);

            foreach (var vertex in mesh.Vertices)
                vertex.SetTag(wrapV(vertex), wrap, !wrap);
            foreach (var halfEdge in mesh.HalfEdges)
                halfEdge.SetTag(wrapE(halfEdge), wrap, !wrap);
            foreach (var face in mesh.Faces)
                face.SetTag(wrapF(face), wrap, !wrap);
        }
        #endregion
    }
}
