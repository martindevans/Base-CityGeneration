using Placeholder.AI.Pathfinding.Graph;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class HalfEdge
        :IEdge
    {
        #region fields
        /// <summary>
        /// The oppositely oriented adjacent half-edge
        /// </summary>
        public HalfEdge Pair { get; internal set; }

        private readonly Vertex _end;
        /// <summary>
        /// Vertex at the end of this half-edge
        /// </summary>
        public Vertex EndVertex
        {
            get { return _end; }
        }

        /// <summary>
        /// The face that this half edge is a border of
        /// </summary>
        public Face Face { get; internal set; }

        /// <summary>
        /// The next half-edge around the face
        /// </summary>
        public HalfEdge Next { get; internal set; }

        public bool IsPrimaryEdge { get; private set; }

        private readonly Mesh _mesh;

        internal IHalfEdgeBuilder Builder;
        #endregion

        #region constructor
        public HalfEdge(Mesh m, Vertex end, bool isPrimary)
        {
            _mesh = m;
            _end = end;
            IsPrimaryEdge = isPrimary;
        }
        #endregion

        /// <summary>
        /// Gets the vertices which bound this half edge
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public void GetVertices(out Vertex start, out Vertex end)
        {
            start = Pair.EndVertex;
            end = EndVertex;
        }

        /// <summary>
        /// Gets the faces on either side of this edge
        /// </summary>
        /// <param name="a">The face this edge bounds</param>
        /// <param name="b">The face the pair of this edge bounds</param>
        public void GetFaces(out Face a, out Face b)
        {
            a = Face;
            b = Pair.Face;
        }

        /// <summary>
        /// calculates if this HalfEdge connects A to B.
        /// </summary>
        /// <param name="start">Vertex at the start</param>
        /// <param name="end">Vertex at the end</param>
        /// <returns>true, if this edge runs from A to B, false if this edge has no pair, or does not connect the given vertices</returns>
        public bool Connects(Vertex start, Vertex end)
        {
            if (Pair == null)
                return false;
            return EndVertex.Equals(end) && Pair.EndVertex.Equals(start);
        }

        public override bool Equals(object obj)
        {
            var a = obj as HalfEdge;
            if (a != null)
                return Equals(a);

            return ReferenceEquals(this, obj);
        }

        public bool Equals(HalfEdge e)
        {
            Vertex a;
            Vertex b;
            GetVertices(out a, out b);

            Vertex x;
            Vertex y;
            e.GetVertices(out x, out y);

            return a.Equals(x) && b.Equals(y);
        }

        public override int GetHashCode()
        {
            Vertex a;
            Vertex b;
            GetVertices(out a, out b);
            return a.GetHashCode() ^ b.GetHashCode();
        }

        /// <summary>
        /// Insert a new vertex into the middle of this edge
        /// </summary>
        /// <param name="middle"></param>
        /// <param name="am"></param>
        /// <param name="mb"></param>
        public void Split(Vertex middle, out HalfEdge am, out HalfEdge mb)
        {
            _mesh.Split(this, middle, out am, out mb);
        }

        public override string ToString()
        {
            return Pair.EndVertex + "=>" + EndVertex;
        }

        IVertex IEdge.Start
        {
            get { return Pair.EndVertex; }
        }

        IVertex IEdge.End
        {
            get { return EndVertex; }
        }

        float IEdge.TraversalCost
        {
            get { return (EndVertex.Position - Pair.EndVertex.Position).Length(); }
        }
    }
}
