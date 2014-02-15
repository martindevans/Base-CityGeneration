using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Mesh
    {
        private readonly HashSet<Face> _faces = new HashSet<Face>();

        /// <summary>
        /// Maps from vertex to the list of edges starting at that vertex
        /// </summary>
        private readonly Dictionary<Vertex, HashSet<HalfEdge>> _halfEdges = new Dictionary<Vertex, HashSet<HalfEdge>>();

        public IEnumerable<Face> Faces
        {
            get { return _faces; }
        }

        public IEnumerable<HalfEdge> HalfEdges
        {
            get { return _halfEdges.Values.SelectMany(a => a); }
        }

        public IEnumerable<Vertex> Vertices
        {
            get { return _halfEdges.Keys; }
        }

        public HalfEdge GetOrConstructHalfEdge(Vertex start, Vertex end)
        {
            if (start.Equals(end))
                throw new InvalidOperationException("Attempted to create a degenerate edge");
            if (start.Mesh != this)
                throw new ArgumentException("start");

            var edgeList = _halfEdges[start];
            var edge = (from e in edgeList
                        where e.EndVertex.Equals(end)
                        select e).SingleOrDefault();

            if (edge == null)
            {
                edge = new HalfEdge(this, end, true);
                HalfEdge pair = new HalfEdge(this, start, false);
                edge.Pair = pair;
                pair.Pair = edge;

                bool addedA = edgeList.Add(edge);
                bool addedB = _halfEdges[end].Add(pair);

                if (!addedA || !addedB)
                    throw new InvalidOperationException("Constructing new half edge found duplicate edge");
            }

            return edge;
        }

        public HalfEdge GetHalfEdge(Vertex start, Vertex end)
        {
            if (start.Equals(end))
                throw new InvalidOperationException("Attempted to create a degenerate edge");
            if (start.Mesh != this)
                throw new ArgumentException("start");

            var edgeList = _halfEdges[start];
            return (from e in edgeList
                        where e.EndVertex.Equals(end)
                        select e).Single();
        }

        internal void Split(HalfEdge edge, Vertex middle, out HalfEdge am, out HalfEdge mb)
        {
            // A --------> B
            // A --> m ++> B

            var a = edge.Pair.EndVertex;
            var b = edge.EndVertex;

            //Find the edges in the faces pointing at the two halves of this half edge
            HalfEdge edgeBeforeEdge = null;
            if (edge.Face != null)
                edgeBeforeEdge = edge.Face.Edges.Single(e => e.Next.Equals(edge));
            HalfEdge edgeBeforeEdgePair = null;
            if (edge.Pair.Face != null)
                edgeBeforeEdgePair = edge.Pair.Face.Edges.Single(e => e.Next.Equals(edge.Pair));

            //delete existing edge
            if (!_halfEdges[b].Remove(edge.Pair))
                throw new InvalidOperationException("Detaching edge from vertex failed");
            if (!_halfEdges[a].Remove(edge))
                throw new InvalidOperationException("Detaching edge from vertex failed");

            //Construct two new edges
            am = GetOrConstructHalfEdge(a, middle);
            mb = GetOrConstructHalfEdge(middle, b);

            if (ReferenceEquals(am, edge) || ReferenceEquals(mb, edge))
                throw new InvalidOperationException("creating new edge fetched an existing edge");

            //Update next pointers in one direction
            if (edgeBeforeEdge != null)
                edgeBeforeEdge.Next = am;
            am.Next = mb;
            mb.Next = edge.Next;

            //Update next pointers in other direction
            if (edgeBeforeEdgePair != null)
                edgeBeforeEdgePair.Next = mb.Pair;
            mb.Pair.Next = am.Pair;
            am.Pair.Next = edge.Pair.Next;

            //Update faces to point at the newly created edges
            if (edge.Face != null)
                edge.Face.Edge = am;
            if (edge.Pair.Face != null)
                edge.Pair.Face.Edge = am.Pair;

            //Update edges to point at face
            am.Face = edge.Face;
            am.Pair.Face = edge.Pair.Face;
            mb.Face = edge.Face;
            mb.Pair.Face = edge.Pair.Face;
        }

        public Vertex GetOrConstructVertex(Vector2 vector2)
        {
            var existing = Vertices.SingleOrDefault(k => k.Position == vector2);
            if (existing != null)
                return existing;

            var v = new Vertex(this, vector2);
            _halfEdges.Add(v, new HashSet<HalfEdge>());
            return v;
        }

        public Vertex GetVertex(Vector2 vector2)
        {
            return _halfEdges.Keys.Single(k => k.Position == vector2);
        }

        public Face GetOrConstructFace(params Vertex[] vertices)
        {
            var edges = new List<HalfEdge>();
            var faces = new HashSet<Face>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var n = vertices[(i + 1) % vertices.Length];
                var e = GetOrConstructHalfEdge(v, n);

                edges.Add(e);
                if (e.Face != null)
                    faces.Add(e.Face);
            }

            if (faces.Count > 1)
                throw new InvalidOperationException("Some edges are already connected to a different face");

            if (faces.Count == 1)
                return faces.Single();

            //Create new face
            Face f = new Face(this) { Edge = edges.First() };
            _faces.Add(f);

            //Connect edges to new face
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                e.Face = f;
                e.Next = edges[(i + 1) % edges.Count];
            }
            return f;
        }

        internal void Delete(Face f)
        {
            if (f.Mesh != this || !_faces.Contains(f))
                throw new InvalidOperationException("Face is not part of this mesh, cannot delete it");

            var edges = f.Edges.ToArray();
            foreach (var halfEdge in edges)
            {
                if (halfEdge.Face != f)
                    throw new InvalidOperationException("Face edge does not point at correct face");
                halfEdge.Face = null;
                halfEdge.Next = null;
            }

            _faces.Remove(f);
        }

        internal IEnumerable<HalfEdge> EdgesFromVertex(Vertex v)
        {
            return _halfEdges[v];
        }

        public Vertex FindClosestVertex(Vector2 v)
        {
            float shortest = float.MaxValue;
            Vertex closest = null;
            foreach (var vertex in Vertices)
            {
                var d = (v - vertex.Position).LengthSquared();
                if (d < shortest)
                {
                    shortest = d;
                    closest = vertex;
                }
            }
            return closest;
        }
    }
}
