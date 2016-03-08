﻿using System.Diagnostics.Contracts;
using Placeholder.AI.Pathfinding.Graph;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    /// <summary>
    /// Half of a directed edge. The other half is directed in the opposite direction and can be found in the Pair property
    /// </summary>
    public class HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>
        :IEdge
    {
        #region fields
        private HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> _pair;
        /// <summary>
        /// The oppositely oriented adjacent half-edge
        /// </summary>
        public HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> Pair { get { return _pair; } }

        private readonly Vertex<TVertexTag, THalfEdgeTag, TFaceTag> _end;
        /// <summary>
        /// Vertex at the end of this half-edge
        /// </summary>
        public Vertex<TVertexTag, THalfEdgeTag, TFaceTag> EndVertex
        {
            get
            {
                Contract.Ensures(Contract.Result<Vertex<TVertexTag, THalfEdgeTag, TFaceTag>>() != null);
                return _end;
            }
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
        public HalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
            : this(end, (HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>)null)
        {
            Contract.Requires(start != null);
            Contract.Requires(end != null);

            IsPrimaryEdge = true;
            _pair = new HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>(start, this);
        }

        private HalfEdge(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end, HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> pair)
        {
            Contract.Requires(end != null);

            _end = end;
            IsPrimaryEdge = false;
        }
        #endregion

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_end != null);
        }

        /// <summary>
        /// Gets the vertices which bound this half edge
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        private void GetVertices(out Vertex<TVertexTag, THalfEdgeTag, TFaceTag> start, out Vertex<TVertexTag, THalfEdgeTag, TFaceTag> end)
        {
            Contract.Requires(Pair != null);
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
            Contract.Requires(Pair != null);
            Contract.Requires(Face != null);
            Contract.Ensures(Contract.ValueAtReturn<Face<TVertexTag, THalfEdgeTag, TFaceTag>>(out a) != null);
            Contract.Ensures(Contract.ValueAtReturn<Face<TVertexTag, THalfEdgeTag, TFaceTag>>(out b) != null);

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
            Contract.Assume(Pair != null);

            var a = obj as HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>;
            if (a != null)
                return Equals(a);

            return ReferenceEquals(this, obj);
        }

        public bool Equals(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> e)
        {
            Contract.Requires(e == null|| e.Pair != null);
            Contract.Requires(Pair != null);

            if (e == null)
                return false;

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
            return 31 + (IsPrimaryEdge ? 17 : -29)
                   * 11 * EndVertex.GetHashCode();
        }

        public override string ToString()
        {
            Contract.Assume(Pair != null);
            return Pair.EndVertex + "=>" + EndVertex;
        }

        #region Pathfinding IEdge
        IVertex IEdge.Start
        {
            get
            {
                Contract.Assume(Pair != null);
                return Pair.EndVertex;
            }
        }

        IVertex IEdge.End
        {
            get { return EndVertex; }
        }

        float IEdge.TraversalCost
        {
            get
            {
                Contract.Assume(Pair != null);
                return (EndVertex.Position - Pair.EndVertex.Position).Length();
            }
        }

        public LineSegment2 Segment
        {
            get { return new LineSegment2(Pair.EndVertex.Position, EndVertex.Position); }
        }
        #endregion
    }
}
