using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using SharpYaml.Tokens;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Mesh<TVertexTag, THalfEdgeTag, TFaceTag>
    {
        #region fields and properties
        private readonly HashSet<Face<TVertexTag, THalfEdgeTag, TFaceTag>> _faces = new HashSet<Face<TVertexTag, THalfEdgeTag, TFaceTag>>();

        private const float VERTEX_EPSILON = 0.05f;

        /// <summary>
        /// Maps from vertex to the list of edges starting at that vertex
        /// </summary>
        private readonly Dictionary<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>, HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>> _halfEdges = new Dictionary<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>, HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>>();

        public IEnumerable<Face<TVertexTag, THalfEdgeTag, TFaceTag>> Faces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Face<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<Face<TVertexTag, THalfEdgeTag, TFaceTag>>>(), a => a != null));
                return _faces;
            }
        }

        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> HalfEdges
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>>(), a => a != null));
                return _halfEdges.Values.SelectMany(a => a);
            }
        }

        public IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>> Vertices
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>>(), a => a != null));
                return _halfEdges.Keys;
            }
        }
        #endregion

        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> GetOrConstructHalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(end != null);
            Contract.Ensures(Contract.Result<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>() != null);

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
                var pair = new HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>(start, false);
                edge.Pair = pair;
                pair.Pair = edge;

                var addedA = edgeList.Add(edge);
                var addedB = _halfEdges[end].Add(pair);

                if (!addedA || !addedB)
                    throw new InvalidOperationException("Constructing new half edge found duplicate edge");
            }

            return edge;
        }

        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> GetHalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(end != null);

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
            Contract.Requires(edge != null && edge.Pair != null);
            Contract.Requires(middle != null);
            Contract.Ensures(Contract.ValueAtReturn<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>(out am) != null);
            Contract.Ensures(Contract.ValueAtReturn<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>(out mb) != null);

            // A --------> B
            // A --> m --> B

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
            Contract.Ensures(Contract.Result<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>() != null);

            var existing = Vertices.Select(k => new { k, d = Vector2.DistanceSquared(k.Position, vector2) }).Where(a => a.d < VERTEX_EPSILON).OrderBy(a => a.d).Select(a => a.k).FirstOrDefault();
            if (existing != null)
                return existing;

            var v = new Vertex<TVertexTag, THalfEdgeTag, TFaceTag>(this, vector2);
            _halfEdges.Add(v, new HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>());
            return v;
        }

        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> GetVertex(Vector2 vector2)
        {
            Contract.Ensures(Contract.Result<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>() != null);

            return _halfEdges.Keys.Single(k => k.Position == vector2);
        }

        public Face<TVertexTag, THalfEdgeTag, TFaceTag> GetOrConstructFace(params Vertex<TVertexTag, THalfEdgeTag, TFaceTag>[] vertices)
        {
            Contract.Requires(vertices != null);
            Contract.Requires(vertices.Length >= 3);
            Contract.Ensures(Contract.Result<Face<TVertexTag, THalfEdgeTag, TFaceTag>>() != null);

            var edges = new List<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>();
            var faces = new HashSet<Face<TVertexTag, THalfEdgeTag, TFaceTag>>();
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var n = vertices[(i + 1) % vertices.Length];
                var e = GetOrConstructHalfEdge(v, n);

                edges.Add(e);
                if (e.Face != null)
                    faces.Add(e.Face);
            }

            Contract.Assert(edges.Count == vertices.Length);

            if (faces.Count > 1)
                throw new InvalidOperationException("Some edges are already connected to a different face");

            if (faces.Count == 1)
                return faces.Single();

            //Create new face
            var f = new Face<TVertexTag, THalfEdgeTag, TFaceTag> { Edge = edges.First() };
            _faces.Add(f);

            //Connect edges to new face
            for (var i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                e.Face = f;
                e.Next = edges[(i + 1) % edges.Count];
            }
            return f;
        }

        internal void Delete(Face<TVertexTag, THalfEdgeTag, TFaceTag> f)
        {
            Contract.Requires(f != null);

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
            Contract.Requires(v != null);

            return _halfEdges[v];
        }

        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> FindClosestVertex(Vector2 v)
        {
            var shortest = float.MaxValue;
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

        /// <summary>
        /// Split a face
        /// </summary>
        /// <param name="face">Face to split</param>
        /// <param name="vertex1">Vertex (somewhere on the edge of the face being split) to split from</param>
        /// <param name="vertex2">Vertex (somewhere on the edge of the face being split) to split to</param>
        /// <param name="result1">Resulting face next to the new edge</param>
        /// <param name="result2">Resulting face next to the new paired edge</param>
        public void Split(Face<TVertexTag, THalfEdgeTag, TFaceTag> face, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> vertex1, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> vertex2, out Face<TVertexTag, THalfEdgeTag, TFaceTag> result1, out Face<TVertexTag, THalfEdgeTag, TFaceTag> result2)
        {
            Contract.Requires(face != null);
            Contract.Requires(vertex1 != null);
            Contract.Requires(vertex2 != null);
            Contract.Ensures(Contract.ValueAtReturn(out result1) != null);
            Contract.Ensures(Contract.ValueAtReturn(out result2) != null);

            //Sanity check: vertex1/2 must not be already connected
            if (vertex1.Edges.Any(e => e.EndVertex.Equals(vertex2)))
                throw new InvalidOperationException("Face split vertices are already connected");

            //Get the vertices from the face (eager)
            var vertices = face.Vertices.ToArray();

            //Sanity check: vertex1/2 must already be in the border of the face we're splitting
            var v1 = Array.FindIndex(vertices, v => v.Equals(vertex1));
            var v2 = Array.FindIndex(vertices, v => v.Equals(vertex2));
            if (v1 < 0 || v2 < 0)
                throw new InvalidOperationException("Face split vertices are not on the border of the face being split");

            //delete face, *cannot access face after this point*
            Delete(face);

            //Swap the indices so we're always doing the indexing into the array in a consistent way
            bool swap = v1 > v2;
            if (swap)
            {
                var tmp = v1;
                v1 = v2;
                v2 = tmp;
            }

            //Face on the right hand side simple goes around the edge and connects A -> ??? -> v2 -> v1 -> ??? -> A
            result2 = GetOrConstructFace(new ArraySegment<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>(vertices, v1, v2 - v1 + 1).ToArray());

            //Face on the right hand side simple goes around the edgeand connects A -> ??? -> v1 -> v2 -> ??? -> A
            var vArr = new Vertex<TVertexTag, THalfEdgeTag, TFaceTag>[vertices.Length - (v2 - v1) + 1];
            Array.Copy(vertices, 0, vArr, 0, v1 + 1);
            Array.Copy(vertices, v2, vArr, v1 + 1, vertices.Length - v2);
            result1 = GetOrConstructFace(vArr);

            if (swap)
            {
                var tmp = result1;
                result1 = result2;
                result2 = tmp;
            }
        }

        public void Transform(Func<Vector2, Vector2> transform)
        {
            //Copy all the values out from halfEdges dictionary
            //We're about to mutate on a field which is in the hashcode and break the entire data structure
            var index = _halfEdges.ToArray();
            _halfEdges.Clear();

            //We can't use the this.Vertices property because that's backed by the _halfEdges collection which we just broke!
            foreach (var vertex in index)
                vertex.Key.Transform(transform);

            //Rebuild index
            foreach (var keyValuePair in index)
                _halfEdges.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }
}
