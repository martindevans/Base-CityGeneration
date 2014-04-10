﻿using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Parcelling;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class ParcelExtensions
    {
        /// <summary>
        /// Given the leaves of a binary tree of parcels, generate a halfedge mesh
        /// </summary>
        /// <typeparam name="TVertexTag"></typeparam>
        /// <typeparam name="TFaceTag"></typeparam>
        /// <param name="leaves"></param>
        /// <returns></returns>
        public static Mesh<TVertexTag, string[], TFaceTag> ToMeshFromBinaryTree<TVertexTag, TFaceTag>(this IEnumerable<Parcel> leaves)
        {
            Parcel root;
            var childrenMap = MapChildren(leaves, out root);

            //We know this is a binary tree - this means every node was created by splitting a parent node in two
            //Create the mesh by creating the root face, and then splitting it in line with the data set we just built up.

            Mesh<TVertexTag, string[], TFaceTag> mesh = new Mesh<TVertexTag, string[], TFaceTag>();
            var rootFace = mesh.GetOrConstructFace(root.Points().Select(mesh.GetOrConstructVertex).ToArray());

            SplitFace(root, rootFace, mesh, childrenMap);

            return mesh;
        }

        private static Dictionary<Parcel, HashSet<Parcel>> MapChildren(IEnumerable<Parcel> leaves, out Parcel root)
        {
            Dictionary<Parcel, HashSet<Parcel>> childrenMap = new Dictionary<Parcel, HashSet<Parcel>>();

            root = null;

            Queue<Parcel> todo = new Queue<Parcel>(leaves);
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

                    HashSet<Parcel> children;
                    if (!childrenMap.TryGetValue(p.Parent, out children))
                    {
                        children = new HashSet<Parcel>();
                        childrenMap.Add(p.Parent, children);
                    }

                    children.Add(p);
                    if (children.Count > 2)
                        throw new InvalidOperationException("Found more than 2 children for a node, but tree is meant to be binary");
                }
            }
            return childrenMap;
        }

        private static void SplitFace<TVertexTag, TFaceTag>(Parcel parcel, Face<TVertexTag, string[], TFaceTag> face, Mesh<TVertexTag, string[], TFaceTag> mesh, Dictionary<Parcel, HashSet<Parcel>> childrenMap)
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

            HashSet<Parcel> children;
            if (!childrenMap.TryGetValue(parcel, out children))
            {
                TagEdges<TVertexTag, TFaceTag>(face, parcel);

                return;
            }

            //Get points, and create/get vertices
            var pa = children.First();
            var a = pa.Points();
            var av = a.Select(mesh.GetOrConstructVertex).ToArray();
            var pb = children.Skip(1).First();
            var b = pb.Points();
            var bv = b.Select(mesh.GetOrConstructVertex).ToArray();

            //Make sure that new points for both shapes are the same
            var newA = new HashSet<Vertex<TVertexTag, string[], TFaceTag>>(av.Where(v => !v.Edges.Any()).OrderBy(v => a.GetHashCode()));
            var newB = new HashSet<Vertex<TVertexTag, string[], TFaceTag>>(bv.Where(v => !v.Edges.Any()).OrderBy(v => a.GetHashCode()));
            if (!newA.IsSubsetOf(newB) && newB.IsSubsetOf(newA))
                throw new InvalidOperationException("Generated vertices for paired parcels did not match up");

            //Insert new vertices into existing face
            InsertVertices(mesh, face, av);
            InsertVertices(mesh, face, bv);

            var faceAVertices = TraceEdges(face, av, bv).ToArray();
            var faceBVertices = TraceEdges(face, bv, av).ToArray();

            mesh.Delete(face);

            var faceA = mesh.GetOrConstructFace(faceAVertices);
            var faceB = mesh.GetOrConstructFace(faceBVertices);

            SplitFace(pa, faceA, mesh, childrenMap);
            SplitFace(pb, faceB, mesh, childrenMap);
        }

        private static void TagEdges<TVertexTag, TFaceTag>(Face<TVertexTag, string[], TFaceTag> face, Parcel parcel)
        {
            foreach (var halfEdge in face.Edges)
            {
                foreach (var edge in parcel.Edges)
                {
                    if (Math.Abs(Vector2.Dot(Vector2.Normalize(edge.End - edge.Start), Vector2.Normalize(halfEdge.EndVertex.Position - halfEdge.Pair.EndVertex.Position)) - 1) < float.Epsilon)
                        halfEdge.Tag = edge.Resources;
                }
            }
        }

        private static IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> TraceEdges<TVertexTag, THalfEdgeTag, TFaceTag>(Face<TVertexTag, THalfEdgeTag, TFaceTag> face, Vertex<TVertexTag, THalfEdgeTag, TFaceTag>[] include, Vertex<TVertexTag, THalfEdgeTag, TFaceTag>[] exclude)
        {
            List<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> output = new List<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>();

            for (int i = 0; i < include.Length; i++)
            {
                var vertex = include[i];
                var next = include[(i + 1) % include.Length];
                if (exclude.Contains(vertex) && exclude.Contains(next))
                    output.Add(vertex);
                else
                {
                    var outwardEdge = face.Edges.Single(e => e.Pair.EndVertex.Equals(vertex));
                    do
                    {
                        output.Add(outwardEdge.Pair.EndVertex);
                        outwardEdge = outwardEdge.Next;
                    } while (!outwardEdge.Pair.EndVertex.Equals(next));
                }
            }

            return output;
        }

        private static void InsertVertices<TVertexTag, THalfEdge, TFaceTag>(this Mesh<TVertexTag, THalfEdge, TFaceTag> mesh, Face<TVertexTag, THalfEdge, TFaceTag> face, IEnumerable<Vertex<TVertexTag, THalfEdge, TFaceTag>> vertices)
        {
            foreach (var v in vertices)
            {
                //If this vertex has edges then we don't care, it's already inserted
                if (v.Edges.Any())
                    continue;

                //Get the next and previous vertices according to this new shape
                var vertex = v;

                //Find halfedge this vertex lies on
                var e = FindHalfEdge(face, vertex.Position);

                //Split the edge and insert this point
                HalfEdge<TVertexTag, THalfEdge, TFaceTag> am, mb;
                mesh.Split(e, vertex, out am, out mb);
            }
        }

        private static HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> FindHalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>(Face<TVertexTag, THalfEdgeTag, TFaceTag> face, Vector2 point)
        {
            HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> winner = null;
            float score = float.MaxValue;
            foreach (var halfEdge in face.Edges)
            {
                var closest = Geometry2D.ClosestPointOnLineSegment(new LineSegment2D(halfEdge.EndVertex.Position, halfEdge.Pair.EndVertex.Position), point);
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
