using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Parcelling;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class ParcelExtensions
    {
        /// <summary>
        /// Given the leaves of a binary tree of parcels, generate a halfedge mesh
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TVertexTag"></typeparam>
        /// <typeparam name="THalfEdgeTag"></typeparam>
        /// <typeparam name="TFaceTag"></typeparam>
        /// <param name="leaves"></param>
        /// <returns></returns>
        public static Mesh<TVertexTag, THalfEdgeTag, TFaceTag> ToMeshFromBinaryTree<T, TVertexTag, THalfEdgeTag, TFaceTag>(this IEnumerable<Parcel<T>> leaves) where T : class, IParcelElement<T>
        {
            Parcel<T> root;
            var childrenMap = MapChildren<T>(leaves, out root);

            //We know this is a binary tree - this means every node was created by splitting a parent node in two
            //Create the mesh by creating the root face, and then splitting it in line with the data set we just built up.

            Mesh<TVertexTag, THalfEdgeTag, TFaceTag> mesh = new Mesh<TVertexTag, THalfEdgeTag, TFaceTag>();
            var rootFace = mesh.GetOrConstructFace(root.Points().Select(mesh.GetOrConstructVertex).ToArray());

            SplitFace(root, rootFace, mesh, childrenMap);

            return mesh;
        }

        private static Dictionary<Parcel<T>, HashSet<Parcel<T>>> MapChildren<T>(IEnumerable<Parcel<T>> leaves, out Parcel<T> root) where T : class, IParcelElement<T>
        {
            Dictionary<Parcel<T>, HashSet<Parcel<T>>> childrenMap = new Dictionary<Parcel<T>, HashSet<Parcel<T>>>();

            root = null;

            Queue<Parcel<T>> todo = new Queue<Parcel<T>>(leaves);
            while (todo.Count > 0)
            {
                var p = todo.Dequeue();

                if (p.Edges.Length != 4)
                    throw new InvalidOperationException("Parcels must have 4 sides");

                if (p.Parent == null)
                {
                    if (root != null && root != p)
                        throw new InvalidOperationException("Found two roots, but tree is meant to be binary");

                    root = p;
                }
                else
                {
                    todo.Enqueue(p.Parent);

                    HashSet<Parcel<T>> children;
                    if (!childrenMap.TryGetValue(p.Parent, out children))
                    {
                        children = new HashSet<Parcel<T>>();
                        childrenMap.Add(p.Parent, children);
                    }

                    children.Add(p);
                    if (children.Count > 2)
                        throw new InvalidOperationException("Found more than 2 children for a node, but tree is meant to be binary");
                }
            }
            return childrenMap;
        }

        private static void SplitFace<T, TVertexTag, THalfEdgeTag, TFaceTag>(Parcel<T> parcel, Face<TVertexTag, THalfEdgeTag, TFaceTag> face, Mesh<TVertexTag, THalfEdgeTag, TFaceTag> mesh, Dictionary<Parcel<T>, HashSet<Parcel<T>>> childrenMap) where T : class, IParcelElement<T>
        {
            //We have a face, like:
            //
            //  A - ? - - - ? - B
            //  |               |
            //  ?               ?
            //  |               |
            //  C - ? - - - ? - D
            //
            // i.e. It may have more than 4 vertices
            //
            // Split by two four sided parcels, like:
            //
            //  B - - X - - C
            //  |     |     |
            //  |     |     |
            //  A - - Y - - D
            //
            // So when we get vertices for the parcels, we should find 4 which we already had and 2 new ones
            //
            // To perform this update we need to:
            // 1. Work out which one is X and which is Y
            // 2. Split BC and DA with X and Y respectively
            // 3. Delete face ABCD
            // 4. Create face A?BXYA and C?DYX

            HashSet<Parcel<T>> children;
            if (!childrenMap.TryGetValue(parcel, out children))
                return;

            //Get points, and create/get vertices
            var pa = children.First();
            var a = pa.Points();
            var av = a.Select(mesh.GetOrConstructVertex).ToArray();
            var pb = children.Skip(1).First();
            var b = pb.Points();
            var bv = b.Select(mesh.GetOrConstructVertex).ToArray();

            //Make sure that new points for both shapes are the same
            var newA = new HashSet<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>(av.Where(v => !v.Edges.Any()).OrderBy(v => a.GetHashCode()));
            var newB = new HashSet<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>(bv.Where(v => !v.Edges.Any()).OrderBy(v => a.GetHashCode()));
            if (!newA.IsSubsetOf(newB) && newB.IsSubsetOf(newA))
                throw new InvalidOperationException("Generated vertices for paired parcels did not match up");

            //Two new points
            var x = newA.First();
            var y = newA.Skip(1).First();

            //Edges to split
            var bc = FindHalfEdge(face, x.Position);
            var da = FindHalfEdge(face, y.Position);

            HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> bx, xc;
            mesh.Split(bc, x, out bx, out xc);

            HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> dy, ya;
            mesh.Split(da, y, out dy, out ya);

            //All vertices from A to B (inclusive)
            var a2b = face.Edges.Append(face.Edges).SkipWhile(e => !Equals(e.Pair.EndVertex, ya.EndVertex)).TakeWhile(e => !Equals(e.Pair.EndVertex, bx.Pair.EndVertex)).Select(e => e.EndVertex).Prepend(ya.EndVertex).ToArray();
            //All vertices from C to D (inclusive)
            var c2d = face.Edges.Append(face.Edges).SkipWhile(e => !Equals(e.Pair.EndVertex, xc.EndVertex)).TakeWhile(e => !Equals(e.Pair.EndVertex, dy.Pair.EndVertex)).Select(e => e.EndVertex).Prepend(xc.EndVertex).ToArray();

            //Delete old face
            mesh.Delete(face);

            //Construct A - ? - B - X - Y
            var a_bxy = mesh.GetOrConstructFace(a2b.Append(bx.EndVertex, dy.EndVertex).ToArray());

            //Construct C - ? - D - Y - X
            var c_dyx = mesh.GetOrConstructFace(c2d.Append(dy.EndVertex, bx.EndVertex).ToArray());

            SplitFace(pa, a_bxy, mesh, childrenMap);
            SplitFace(pb, c_dyx, mesh, childrenMap);
        }

        private static HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> FindHalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>(Face<TVertexTag, THalfEdgeTag, TFaceTag> face, Vector2 point)
        {
            HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> winner = null;
            float score = float.MaxValue;
            foreach (var halfEdge in face.Edges)
            {
                var closest = Geometry2D.ClosestPointOnLineSegment(halfEdge.EndVertex.Position, halfEdge.Pair.EndVertex.Position, point);
                var d = Vector2.DistanceSquared(closest, point);
                if (d < score)
                {
                    winner = halfEdge;
                    score = d;
                }
            }

            if (score > 0.05f)
                throw new InvalidOperationException("Closest edge is too far");

            return winner;
        }
    }
}
