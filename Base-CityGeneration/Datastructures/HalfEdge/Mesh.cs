using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using HandyCollections.Geometry;
using Placeholder.AI.Pathfinding.Graph.NavigationMesh;
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
        private readonly SortedSet<Face<TVTag, TETag, TFTag>> _faces = new SortedSet<Face<TVTag, TETag, TFTag>>(new Face<TVTag, TETag, TFTag>.Comparer());
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

        private int _nextFaceId;
        #endregion

        #region constructors
        public Mesh(float bounds = 1000, int threshold = 10)
        {
            var bound = new BoundingRectangle(-new Vector2(bounds) / 2, new Vector2(bounds) / 2);

            _vertices = new Quadtree<Vertex<TVTag, TETag, TFTag>>(bound, 10);
            _halfEdges = new Quadtree<HalfEdge<TVTag, TETag, TFTag>>(bound, 10);
        }

        public Mesh(BoundingRectangle  bounds, int threshold = 10)
        {
            _vertices = new Quadtree<Vertex<TVTag, TETag, TFTag>>(bounds, 10);
            _halfEdges = new Quadtree<HalfEdge<TVTag, TETag, TFTag>>(bounds, 10);
        }
        #endregion

        #region edges
        public HalfEdge<TVTag, TETag, TFTag> GetOrConstructHalfEdge(Vertex<TVTag, TETag, TFTag> start, Vertex<TVTag, TETag, TFTag> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(start.Mesh == this);
            Contract.Requires(end != null);
            Contract.Requires(end.Mesh == this);
            Contract.Requires(!start.Equals(end));
            Contract.Ensures(Contract.Result<HalfEdge<TVTag, TETag, TFTag>>() != null);
            Contract.Ensures(!Contract.Result<HalfEdge<TVTag, TETag, TFTag>>().IsDeleted);

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
                var bb = edge.Bounds;
                _halfEdges.Insert(bb, edge);
                _halfEdges.Insert(bb, pair);
            }

            Contract.Assert(!edge.IsDeleted);
            return edge;
        }

        public HalfEdge<TVTag, TETag, TFTag> GetHalfEdge(Vertex<TVTag, TETag, TFTag> start, Vertex<TVTag, TETag, TFTag> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(end != null);
            Contract.Ensures(Contract.Result<HalfEdge<TVTag, TETag, TFTag>>() == null || !Contract.Result<HalfEdge<TVTag, TETag, TFTag>>().IsDeleted);

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
            Contract.Requires(edge != null);
            Contract.Requires(!edge.IsDeleted);
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

            //Delete existing edge (but preserve faces, this potentially leaves the mesh in an invalid state if the face was pointing at the edge we're deleting)
            Delete(edge, preserveFaces: true);

            //Construct two new edges
            am = GetOrConstructHalfEdge(a, middle);
            mb = GetOrConstructHalfEdge(middle, b);

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

            //Update faces to point at the newly created edges (fixing potentially invalid state created when the edge was deleted)
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
            Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<HalfEdge<TVTag, TETag, TFTag>>>(), a => !a.IsDeleted));

            return _halfEdges.Intersects(rectangle);
        }

        public void Delete(HalfEdge<TVTag, TETag, TFTag> edge)
        {
            Contract.Requires(edge != null && !edge.IsDeleted);
            Contract.Ensures(edge.IsDeleted);

            Delete(edge, false);
        }

        private void Delete(HalfEdge<TVTag, TETag, TFTag> edge, bool preserveFaces = false)
        {
            DeleteHalf(edge, preserveFaces);
            DeleteHalf(edge.Pair, preserveFaces);
        }

        private void DeleteHalf(HalfEdge<TVTag, TETag, TFTag> edge, bool preserveFaces = false)
        {
            if (!edge.StartVertex.DeleteEdge(edge))
                throw new InvalidOperationException("Detaching edge from vertex failed");
            if (!_halfEdges.Remove(edge.Bounds, edge))
                throw new InvalidOperationException("Failed to remove half edge from spatial index");

            if (!preserveFaces && edge.Face != null)
                Delete(edge.Face);

            edge.IsDeleted = true;
        }
        #endregion

        #region vertices
        /// <summary>
        /// Get the vertex at the given position or, if it does not exist, construct a new vertex
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public Vertex<TVTag, TETag, TFTag> GetOrConstructVertex(Vector2 vector2)
        {
            Contract.Ensures(Contract.Result<Vertex<TVTag, TETag, TFTag>>() != null);
            Contract.Ensures(!Contract.Result<Vertex<TVTag, TETag, TFTag>>().IsDeleted);

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

        /// <summary>
        /// Get the vertex at the given location or null
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns></returns>
        public Vertex<TVTag, TETag, TFTag> GetVertex(Vector2 vector2)
        {
            Contract.Ensures(
                Contract.Result<Vertex<TVTag, TETag, TFTag>>() == null
                || (
                    Contract.Result<Vertex<TVTag, TETag, TFTag>>().Position == vector2
                    && !Contract.Result<Vertex<TVTag, TETag, TFTag>>().IsDeleted
                )
            );

            return _vertices
                .Intersects(new BoundingRectangle(vector2, vector2).Inflate(0.1f))
                .SingleOrDefault(a => a.Position == vector2);
        }

        /// <summary>
        /// Find the closest vertex to the given point
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Find all vertices in the given rectangle
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public IEnumerable<Vertex<TVTag, TETag, TFTag>> FindVertices(BoundingRectangle rectangle)
        {
            Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<Vertex<TVTag, TETag, TFTag>>>(), a => a != null && !a.IsDeleted));

            //We need to do intersection then containment because the bounding box for a vertex is slightly larger than the vertex!
            return _vertices
                .Intersects(rectangle)
                .Where(a => rectangle.Contains(a.Position));
        }

        /// <summary>
        /// Delete the given vertex.
        /// This will also delete:
        ///  - This vertex
        ///  - Attached edges (inwards and outwards)
        ///  - All faces which contain this vertex
        /// </summary>
        /// <param name="vertex"></param>
        public void Delete(Vertex<TVTag, TETag, TFTag> vertex)
        {
            Contract.Requires(vertex != null && !vertex.IsDeleted);
            Contract.Ensures(vertex.IsDeleted);

            foreach (var face in vertex.Faces)
                Delete(face);

            foreach (var halfEdge in vertex.Edges.ToArray())
                Delete(halfEdge);

            _vertices.Remove(_vertices.Bounds, vertex);
            vertex.IsDeleted = true;
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
        /// 
        /// </summary>
        /// <param name="constructEdges"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private Face<TVTag, TETag, TFTag> GetOrConstructFace(bool constructEdges, IReadOnlyList<Vertex<TVTag, TETag, TFTag>> vertices)
        {
            Contract.Requires(vertices != null);
            Contract.Requires(vertices.Count >= 3);

            var edges = new List<HalfEdge<TVTag, TETag, TFTag>>();
            Face<TVTag, TETag, TFTag> foundFace = null;
            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                var n = vertices[(i + 1) % vertices.Count];
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

            Contract.Assert(edges.Count == vertices.Count);

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
            var f = new Face<TVTag, TETag, TFTag>(_nextFaceId++){ Edge = edges.First() };
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

        public Face<TVTag, TETag, TFTag> GetOrConstructFace(IReadOnlyList<Vertex<TVTag, TETag, TFTag>> vertices)
        {
            Contract.Requires(vertices != null);
            Contract.Requires(vertices.Count >= 3);
            Contract.Ensures(Contract.Result<Face<TVTag, TETag, TFTag>>() != null);

            var face = GetOrConstructFace(true, vertices);

            Contract.Assert(face != null);

            return face;
        }

        public Face<TVTag, TETag, TFTag> GetOrConstructFace(IReadOnlyList<HalfEdge<TVTag, TETag, TFTag>> edges)
        {
            Contract.Requires(edges != null);
            Contract.Requires(edges.Count >= 3);
            Contract.Ensures(Contract.Result<Face<TVTag, TETag, TFTag>>() != null);

            //If we find an edge already attached to a face while walking the edges keep hold of it for later
            Face<TVTag, TETag, TFTag> foundFace = null;
            for (int i = 0; i < edges.Count; i++)
            {
                var ab = edges[i];
                var bc = edges[(i + 1) % edges.Count];

                //Check that edges lead on from one another
                if (!ab.EndVertex.Equals(bc.StartVertex))
                    throw new ArgumentNullException("edges", "end vertex of one edge is not the start vertex of the next edge");

                //Keep track of attached faces and sanity check
                if (ab.Face != null)
                {
                    if (foundFace != null && !foundFace.Equals(ab.Face))
                        throw new InvalidOperationException("Some edges are already connected to multiple different faces");
                    foundFace = ab.Face;
                }
            }

            //If we found a face let's see if it's the face we want
            //If all the edges of the found face are in the edge set then we're good to go
            if (foundFace != null)
            {
                if (foundFace.Edges.All(edges.Contains))
                    return foundFace;
                else
                    throw new InvalidOperationException("Some edges are already connected to a different face");
            }

            //Create new face
            var f = new Face<TVTag, TETag, TFTag>(_nextFaceId++) { Edge = edges.First() };
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

            var face = GetOrConstructFace((IReadOnlyList<Vertex<TVTag, TETag, TFTag>>)vertices);

            Contract.Assert(face != null);

            return face;
        }

        internal void Delete(Face<TVTag, TETag, TFTag> f)
        {
            Contract.Requires(f != null && !f.IsDeleted);
            Contract.Requires(f.Edges.All(he => he.Face.Equals(f)));
            Contract.Ensures(f.IsDeleted);

            if (!_faces.Remove(f))
                throw new InvalidOperationException("Face was not in face set");

            var start = f.Edge;
            var e = start;
            do
            {
                //Store edge, move to next before mutating
                var p = e;

                //Sanity checks
                if (e == null)
                    throw new InvalidMeshException("Found a null 'Next' pointer while walking around a face");
                if (e.Face == null)
                    throw new InvalidOperationException("Face edge does not point at correct face (null)");
                if (!e.Face.Equals(f))
                    throw new InvalidOperationException("Face edge does not point at correct face");

                //Move to next edge
                e = e.Next;

                //Mutate the edge, which obviously has to be done after reading values and sanity checking
                p.Next = null;
                p.Face = null;

            } while (!ReferenceEquals(e, start));

            //Set face to deleted
            f.Edge = null;
            f.IsDeleted = true;
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
