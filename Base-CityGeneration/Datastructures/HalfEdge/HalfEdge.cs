using System.Diagnostics.Contracts;
using Placeholder.AI.Pathfinding.Graph;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    /// <summary>
    /// Half of a directed edge. The other half is directed in the opposite direction and can be found in the Pair property
    /// </summary>
    public class HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>
        :IEdge
    {
        #region fields
        /// <summary>
        /// The oppositely oriented adjacent half-edge
        /// </summary>
        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> Pair { get; internal set; }

        private readonly Vertex<TVertexTag, THalfEdgeTag, TFaceTag> _end;
        /// <summary>
        /// Vertex at the end of this half-edge
        /// </summary>
        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> EndVertex
        {
            get { return _end; }
        }

        /// <summary>
        /// The face that this half edge is a border of
        /// </summary>
        public Face<TVertexTag, THalfEdgeTag, TFaceTag> Face { get; internal set; }

        /// <summary>
        /// The next half-edge around the face
        /// </summary>
        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> Next { get; internal set; }

        public bool IsPrimaryEdge { get; private set; }

        public THalfEdgeTag Tag;
        #endregion

        #region constructor
        public HalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end, bool isPrimary)
        {
            Contract.Requires(end != null);

            _end = end;
            IsPrimaryEdge = isPrimary;
        }
        #endregion

        /// <summary>
        /// Gets the vertices which bound this half edge
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        private void GetVertices(out Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, out Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
        {
            Contract.Ensures(Contract.ValueAtReturn(out start) != null);
            Contract.Ensures(Contract.ValueAtReturn(out end) != null);

            start = Pair.EndVertex;
            end = EndVertex;
        }

        /// <summary>
        /// Gets the faces on either side of this edge
        /// </summary>
        /// <param name="a">The face this edge bounds</param>
        /// <param name="b">The face the pair of this edge bounds</param>
        public void GetFaces(out Face<TVertexTag, THalfEdgeTag, TFaceTag> a, out Face<TVertexTag, THalfEdgeTag, TFaceTag> b)
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
        public bool Connects(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
        {
            if (Pair == null)
                return false;
            return EndVertex.Equals(end) && Pair.EndVertex.Equals(start);
        }

        public override bool Equals(object obj)
        {
            var a = obj as HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>;
            if (a != null)
                return Equals(a);

            return ReferenceEquals(this, obj);
        }

        public bool Equals(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> e)
        {
            Contract.Requires(e != null);
            Contract.Requires(EndVertex != null);
            Contract.Requires(e.EndVertex != null);

            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> a;
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> b;
            GetVertices(out a, out b);

            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> x;
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> y;
            e.GetVertices(out x, out y);

            return a.Equals(x) && b.Equals(y);
        }

        public override int GetHashCode()
        {
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> a;
            Vertex<TVertexTag, THalfEdgeTag, TFaceTag> b;
            GetVertices(out a, out b);
            return a.GetHashCode() ^ b.GetHashCode();
        }

        public override string ToString()
        {
            return Pair.EndVertex + "=>" + EndVertex;
        }

        #region Pathfinding IEdge
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
        #endregion
    }
}
