using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using HandyCollections.Geometry;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TVTag">Type of tag to attach to vertices</typeparam>
    /// <typeparam name="TETag">Type of tag to attach to half edges</typeparam>
    /// <typeparam name="TFTag">Type of tag to attach to faces</typeparam>
    public class Mesh<TVTag, TETag, TFTag>
    {
        #region fields and properties
        private readonly HashSet<Face<TVTag, TETag, TFTag>> _faces = new HashSet<Face<TVTag, TETag, TFTag>>();
        private readonly Quadtree<Vertex<TVTag, TETag, TFTag>> _vertices;
        private readonly Quadtree<HalfEdge<TVTag, TETag, TFTag>> _halfEdges;  

        private const float VERTEX_EPSILON = 0.05f;

        public IEnumerable<Face<TVTag, TETag, TFTag>> Faces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Face<TVTag, TETag, TFTag>>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<Face<TVTag, TETag, TFTag>>>(), a => a != null));
                return _faces;
            }
        }

        public IEnumerable<HalfEdge<TVTag, TETag, TFTag>> HalfEdges
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TVTag, TETag, TFTag>>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<HalfEdge<TVTag, TETag, TFTag>>>(), a => a != null));
                return _vertices.SelectMany(a => a.Value.Edges);
            }
        }

        public IEnumerable<Vertex<TVTag, TETag, TFTag>> Vertices
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Vertex<TVTag, TETag, TFTag>>>() != null);
                Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<Vertex<TVTag, TETag, TFTag>>>(), a => a != null));
                return _vertices.Select(a => a.Value);
            }
        }
        #endregion

        public Mesh(float bounds = 1000, int threshold = 10)
        {
            var bound = new BoundingRectangle(-new Vector2(bounds) / 2, new Vector2(bounds) / 2);

            _vertices = new Quadtree<Vertex<TVTag, TETag, TFTag>>(bound, 10);
            _halfEdges = new Quadtree<HalfEdge<TVTag, TETag, TFTag>>(bound, 10);
        }

        #region edges
        public HalfEdge<TVTag, TETag, TFTag> GetOrConstructHalfEdge(Vertex<TVTag, TETag, TFTag> start, Vertex<TVTag, TETag, TFTag> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(start.Mesh == this);
            Contract.Requires(end != null);
            Contract.Requires(end.Mesh == this);
            Contract.Requires(!start.Equals(end));
            Contract.Ensures(Contract.Result<HalfEdge<TVTag, TETag, TFTag>>() != null);

            //Try to find an edge which already connects these vertices
            var edge = (from e in start.Edges
                        where e.EndVertex.Equals(end)
                        select e).SingleOrDefault();

            //No luck, create a new edge
            if (edge == null)
            {
                //Create edge and pair and associate with one another
                edge = new HalfEdge<TVTag, TETag, TFTag>(start, end);
                var pair = edge.Pair;

                //Add to vertices
                var addedA = start.AddEdge(edge);
                var addedB = end.AddEdge(pair);
                if (!addedA || !addedB)
                    throw new InvalidOperationException("Constructing new half edge found duplicate edge");

                //Add to quadtree
                var bb = new BoundingRectangle(start.Position, end.Position).Inflate(0.2f);
                _halfEdges.Insert(bb, edge);
                _halfEdges.Insert(bb, pair);
            }

            return edge;
        }

        public HalfEdge<TVTag, TETag, TFTag> GetHalfEdge(Vertex<TVTag, TETag, TFTag> start, Vertex<TVTag, TETag, TFTag> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(end != null);

            if (start.Equals(end))
                throw new InvalidOperationException("Attempted to create a degenerate edge");
            if (start.Mesh != this)
                throw new ArgumentException("start");

            return (from e in start.Edges
                    where e.EndVertex.Equals(end)
                    select e
                   ).SingleOrDefault();
        }

        internal void Split(HalfEdge<TVTag, TETag, TFTag> edge, Vertex<TVTag, TETag, TFTag> middle, out HalfEdge<TVTag, TETag, TFTag> am, out HalfEdge<TVTag, TETag, TFTag> mb)
        {
            Contract.Requires(edge != null && edge.Pair != null);
            Contract.Requires(middle != null);
            Contract.Ensures(Contract.ValueAtReturn<HalfEdge<TVTag, TETag, TFTag>>(out am) != null);
            Contract.Ensures(Contract.ValueAtReturn<HalfEdge<TVTag, TETag, TFTag>>(out mb) != null);

            // A --------> B
            // A --> m --> B

            var a = edge.Pair.EndVertex;
            var b = edge.EndVertex;

            //Find the edges in the faces pointing at the two halves of this half edge
            HalfEdge<TVTag, TETag, TFTag> edgeBeforeEdge = null;
            if (edge.Face != null)
                edgeBeforeEdge = edge.Face.Edges.Single(e => e.Next.Equals(edge));
            HalfEdge<TVTag, TETag, TFTag> edgeBeforeEdgePair = null;
            if (edge.Pair.Face != null)
                edgeBeforeEdgePair = edge.Pair.Face.Edges.Single(e => e.Next.Equals(edge.Pair));

            //Delete existing edge
            if (!b.DeleteEdge(edge.Pair))
                throw new InvalidOperationException("Detaching edge from vertex failed");
            if (!a.DeleteEdge(edge))
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

        /// <summary>
        /// Find edges which itnersect the given bounds
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public IEnumerable<HalfEdge<TVTag, TETag, TFTag>> FindEdges(BoundingRectangle rectangle)
        {
            return _halfEdges.Intersects(rectangle);
        }
        #endregion

        #region vertices
        public Vertex<TVTag, TETag, TFTag> GetOrConstructVertex(Vector2 vector2)
        {
            Contract.Ensures(Contract.Result<Vertex<TVTag, TETag, TFTag>>() != null);

            //Select candidate vertices within range (square query)
            var candidates = _vertices.Intersects(new BoundingRectangle(vector2 - new Vector2(VERTEX_EPSILON / 2), vector2 + new Vector2(VERTEX_EPSILON / 2)));

            //Select best candidate (nearest)
            float bestDistance = float.MaxValue;
            Vertex<TVTag, TETag, TFTag> best = null;
            foreach (var candidate in candidates)
            {
                var d = Vector2.DistanceSquared(candidate.Position, vector2);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }

            //If we found a suitable one, return it
            if (best != null && bestDistance <= VERTEX_EPSILON * VERTEX_EPSILON)
                return best;

            //Otherwise create a new vertex and return that
            var v = new Vertex<TVTag, TETag, TFTag>(this, vector2);
            _vertices.Insert(new BoundingRectangle(v.Position, v.Position).Inflate(0.1f), v);
            return v;
        }

        public Vertex<TVTag, TETag, TFTag> GetVertex(Vector2 vector2)
        {
            Contract.Ensures(Contract.Result<Vertex<TVTag, TETag, TFTag>>() != null);

            return _vertices
                .Intersects(new BoundingRectangle(vector2, vector2).Inflate(0.1f))
                .Single(a => a.Position == vector2);
        }

        private Face<TVTag, TETag, TFTag> GetOrConstructFace(bool constructEdges, params Vertex<TVTag, TETag, TFTag>[] vertices)
        {
            Contract.Requires(vertices != null);
            Contract.Requires(vertices.Length >= 3);

            var edges = new List<HalfEdge<TVTag, TETag, TFTag>>();
            Face<TVTag, TETag, TFTag> foundFace = null;
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var n = vertices[(i + 1) % vertices.Length];
                var e = constructEdges ? GetOrConstructHalfEdge(v, n) : GetHalfEdge(v, n);

                //Can't find an edge for this pair, this will only happen if ~constructEdges
                if (e == null)
                    return null;

                edges.Add(e);
                if (e.Face != null)
                {
                    //If this edge is already connected to a face take note of that
                    //If we've *already* taken such a note (and it wasn't to the same face) we have a problem!
                    if (foundFace != null && !foundFace.Equals(e.Face))
                        throw new InvalidOperationException("Some edges are already connected to multiple different faces");
                    foundFace = e.Face;
                }
            }

            Contract.Assert(edges.Count == vertices.Length);

            //If we found a face let's see if it's the face we want
            //If all the edges of the found face are in out constructed edge set then we're good to go
            if (foundFace != null)
            {
                if (foundFace.Edges.All(edges.Contains))
                    return foundFace;
                else
                    throw new InvalidOperationException("Some edges are already connected to a different face");
            }

            //Create new face
            var f = new Face<TVTag, TETag, TFTag> { Edge = edges.First() };
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

        //public Vertex<TVTag, TETag, TFTag> FindClosestVertex(Vector2 v)
        //{
        //    var shortest = float.MaxValue;
        //    Vertex<TVTag, TETag, TFTag> closest = null;

        //    //Search for the best vertex, initially starting with a small bounds query and progressively expanding it
        //    var size = 10;
        //    var queried = new HashSet<Vertex<TVTag, TETag, TFTag>>();
        //    while (closest == null && queried.Count < _vertices.Count)
        //    {
        //        var bounds = new BoundingRectangle(v - new Vector2(size), v + new Vector2(size));

        //        var vertices = _vertices.Intersects(bounds);

        //        foreach (var vertex in vertices)
        //        {
        //            //Skip this vertex, we've already tried it
        //            if (queried.Contains(vertex))
        //                continue;

        //            //Check all found vertices and pick the closest
        //            var d = (v - vertex.Position).LengthSquared();
        //            if (d < shortest)
        //            {
        //                shortest = d;
        //                closest = vertex;
        //            }
        //        }

        //        if (closest != null)
        //        {
        //            queried.UnionWith(vertices);
        //            size *= 2;
        //        }
        //    }

        //    return closest;
        //}

        public IEnumerable<Vertex<TVTag, TETag, TFTag>> FindVertices(BoundingRectangle rectangle)
        {
            return _vertices.Intersects(rectangle).Where(a => rectangle.Contains(a.Position));
        }
        #endregion

        #region faces
        /// <summary>
        /// Attempt to construct a face connecting the given vertices. Only succeeds if all edges *already* have half edges as appropriate
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public Face<TVTag, TETag, TFTag> TryGetOrConstructFace(params Vertex<TVTag, TETag, TFTag>[] vertices)
        {
            Contract.Requires(vertices != null);
            Contract.Requires(vertices.Length >= 3);

            return GetOrConstructFace(false, vertices);
        }

        /// <summary>
        /// Construct a face connecting the given vertices
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public Face<TVTag, TETag, TFTag> GetOrConstructFace(params Vertex<TVTag, TETag, TFTag>[] vertices)
        {
            Contract.Requires(vertices != null);
            Contract.Requires(vertices.Length >= 3);
            Contract.Ensures(Contract.Result<Face<TVTag, TETag, TFTag>>() != null);

            var face = GetOrConstructFace(true, vertices);

            Contract.Assert(face != null);

            return face;
        }

        internal void Delete(Face<TVTag, TETag, TFTag> f)
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

        /// <summary>
        /// Split a face
        /// </summary>
        /// <param name="face">Face to split</param>
        /// <param name="vertex1">Vertex (somewhere on the edge of the face being split) to split from</param>
        /// <param name="vertex2">Vertex (somewhere on the edge of the face being split) to split to</param>
        /// <param name="result1">Resulting face next to the new edge</param>
        /// <param name="result2">Resulting face next to the new paired edge</param>
        public void Split(Face<TVTag, TETag, TFTag> face, Vertex<TVTag, TETag, TFTag> vertex1, Vertex<TVTag, TETag, TFTag> vertex2, out Face<TVTag, TETag, TFTag> result1, out Face<TVTag, TETag, TFTag> result2)
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
            result2 = GetOrConstructFace(new ArraySegment<Vertex<TVTag, TETag, TFTag>>(vertices, v1, v2 - v1 + 1).ToArray());

            //Face on the right hand side simple goes around the edgeand connects A -> ??? -> v1 -> v2 -> ??? -> A
            var vArr = new Vertex<TVTag, TETag, TFTag>[vertices.Length - (v2 - v1) + 1];
            Array.Copy(vertices, 0, vArr, 0, v1 + 1);
            Array.Copy(vertices, v2, vArr, v1 + 1, vertices.Length - v2);
            result1 = GetOrConstructFace(vArr);

            //if (swap)
            //{
            //    var tmp = result1;
            //    result1 = result2;
            //    result2 = tmp;
            //}
        }
        #endregion

        public void Transform(Func<Vector2, Vector2> transform)
        {
            //Get all vertices and clear the quadtree
            var vertices = _vertices.Select(a => a.Value).ToArray();
            _vertices.Clear();

            //Now modify all the vertices and insert back into quadtree
            foreach (var vertex in vertices)
            {
                vertex.Transform(transform);
                _vertices.Insert(new BoundingRectangle(vertex.Position, vertex.Position).Inflate(0.1f), vertex);
            }
        }
    }
}
