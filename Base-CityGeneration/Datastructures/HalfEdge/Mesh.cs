using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Mesh<TVertexTag, THalfEdgeTag, TFaceTag>
    {
        private readonly HashSet<Face<TVertexTag, THalfEdgeTag, TFaceTag>> _faces = new HashSet<Face<TVertexTag, THalfEdgeTag, TFaceTag>>();

        private const float VERTEX_EPSILON = 0.05f;

        /// <summary>
        /// Maps from vertex to the list of edges starting at that vertex
        /// </summary>
        private readonly Dictionary<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>, HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>> _halfEdges = new Dictionary<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>, HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>>();

        public IEnumerable<Face<TVertexTag, THalfEdgeTag, TFaceTag>> Faces
        {
            get { return _faces; }
        }

        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> HalfEdges
        {
            get { return _halfEdges.Values.SelectMany(a => a); }
        }

        public IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> Vertices
        {
            get { return _halfEdges.Keys; }
        }

        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> GetOrConstructHalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
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
                edge = new HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>(end, true);
                HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> pair = new HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>(start, false);
                edge.Pair = pair;
                pair.Pair = edge;

                bool addedA = edgeList.Add(edge);
                bool addedB = _halfEdges[end].Add(pair);

                if (!addedA || !addedB)
                    throw new InvalidOperationException("Constructing new half edge found duplicate edge");
            }

            return edge;
        }

        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> GetHalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
        {
            if (start.Equals(end))
                throw new InvalidOperationException("Attempted to create a degenerate edge");
            if (start.Mesh != this)
                throw new ArgumentException("start");

            var edgeList = _halfEdges[start];
            return (from e in edgeList
                        where e.EndVertex.Equals(end)
                        select e).SingleOrDefault();
        }

        internal void Split(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edge, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> middle, out HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> am, out HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> mb)
        {
            // A --------> B
            // A --> m ++> B

            var a = edge.Pair.EndVertex;
            var b = edge.EndVertex;

            //Find the edges in the faces pointing at the two halves of this half edge
            HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edgeBeforeEdge = null;
            if (edge.Face != null)
                edgeBeforeEdge = edge.Face.Edges.Single(e => e.Next.Equals(edge));
            HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edgeBeforeEdgePair = null;
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

        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> GetOrConstructVertex(Vector2 vector2)
        {
            var existing = Vertices.Select(k => new { k, d = Vector2.DistanceSquared(k.Position, vector2) }).Where(a => a.d < VERTEX_EPSILON).OrderBy(a => a.d).Select(a => a.k).FirstOrDefault();
            if (existing != null)
                return existing;

            var v = new Vertex<TVertexTag, THalfEdgeTag, TFaceTag>(this, vector2);
            _halfEdges.Add(v, new HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>());
            return v;
        }

        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> GetVertex(Vector2 vector2)
        {
            return _halfEdges.Keys.Single(k => k.Position == vector2);
        }

        public Face<TVertexTag, THalfEdgeTag, TFaceTag> GetOrConstructFace(params Vertex<TVertexTag, THalfEdgeTag, TFaceTag>[] vertices)
        {
            var edges = new List<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>();
            var faces = new HashSet<Face<TVertexTag, THalfEdgeTag, TFaceTag>>();
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
            Face<TVertexTag, THalfEdgeTag, TFaceTag> f = new Face<TVertexTag, THalfEdgeTag, TFaceTag> { Edge = edges.First() };
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

        internal void Delete(Face<TVertexTag, THalfEdgeTag, TFaceTag> f)
        {
            if (!_faces.Contains(f))
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

        internal IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> EdgesFromVertex(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> v)
        {
            return _halfEdges[v];
        }

        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> FindClosestVertex(Vector2 v)
        {
            float shortest = float.MaxValue;
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> closest = null;
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
