using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Placeholder.AI.Pathfinding.Graph;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    /// <summary>
    /// Half of a directed edge. The other half is directed in the opposite direction and can be found in the Pair property
    /// </summary>
    public class HalfEdge<TV, TE, TF>
        : BaseTagged<TE, IHalfEdgeTag<TV, TE, TF>, HalfEdge<TV, TE, TF>>,
          IEdge<Vertex<TV, TE, TF>, HalfEdge<TV, TE, TF>>  
    {
        #region fields
        private readonly HalfEdge<TV, TE, TF> _pair;
        /// <summary>
        /// The oppositely oriented adjacent half-edge
        /// </summary>
        public HalfEdge<TV, TE, TF> Pair
        {
            get
            {
                Contract.Ensures(Contract.Result<HalfEdge<TV, TE, TF>>() != null);
                return _pair;
            }
        }

        private readonly Vertex<TV, TE, TF> _end;
        /// <summary>
        /// Vertex at the end of this half-edge
        /// </summary>
        public Vertex<TV, TE, TF> EndVertex
        {
            get
            {
                Contract.Ensures(Contract.Result<Vertex<TV, TE, TF>>() != null);
                return _end;
            }
        }

        /// <summary>
        /// Vertex at the end of the pair of this half-edge
        /// </summary>
        public Vertex<TV, TE, TF> StartVertex
        {
            get
            {
                Contract.Ensures(Contract.Result<Vertex<TV, TE, TF>>() != null);
                return _pair._end;
            }
        }

        /// <summary>
        /// The face that this half edge is a border of
        /// </summary>
        public Face<TV, TE, TF> Face { get; internal set; }

        /// <summary>
        /// The next half-edge around the face
        /// </summary>
        public HalfEdge<TV, TE, TF> Next { get; internal set; }

        /// <summary>
        /// Enumerate edges around the face (starting with thos edge)
        /// </summary>
        public IEnumerable<HalfEdge<TV, TE, TF>> Around
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TV, TE, TF>>>() != null);

                var next = this;
                do
                {
                    yield return next;
                    next = next.Next;
                } while (!next.Equals(this));
            }
        }

        private readonly bool _isPrimary;
        public bool IsPrimaryEdge { get { return _isPrimary; } }

        public bool IsDeleted { get; internal set; }

        internal BoundingRectangle Bounds
        {
            get
            {
                var min = Vector2.Min(StartVertex.Position, EndVertex.Position);
                var max = Vector2.Max(StartVertex.Position, EndVertex.Position);
                return new BoundingRectangle(min, max).Inflate(0.2f);
            }
        }
        #endregion

        #region constructor
        public HalfEdge(Vertex<TV, TE, TF> start, Vertex<TV, TE, TF> end)
        {
            Contract.Requires(start != null);
            Contract.Requires(end != null);

            _end = end;
            _pair = new HalfEdge<TV, TE, TF>(start, this);
            _isPrimary = true;

            Contract.Assert(_pair != null);
            Contract.Assert(_pair.Pair.Equals(this));
        }

        private HalfEdge(Vertex<TV, TE, TF> end, HalfEdge<TV, TE, TF> pair)
        {
            Contract.Requires(end != null);
            Contract.Requires(pair != null);

            _end = end;
            _pair = pair;
            _isPrimary = false;
        }
        #endregion

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_end != null);
            Contract.Invariant(_pair != null);
            Contract.Invariant(_pair._end != null);
        }

        /// <summary>
        /// Gets the vertices which bound this half edge
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        [Pure]
        private void GetVertices(out Vertex<TV, TE, TF> start, out Vertex<TV, TE, TF> end)
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
        [Pure]
        public void GetFaces(out Face<TV, TE, TF> a, out Face<TV, TE, TF> b)
        {
            Contract.Requires(Face != null);
            Contract.Ensures(Contract.ValueAtReturn<Face<TV, TE, TF>>(out a) != null);
            Contract.Ensures(Contract.ValueAtReturn<Face<TV, TE, TF>>(out b) != null);

            a = Face;
            b = Pair.Face;
        }

        /// <summary>
        /// calculates if this HalfEdge connects A to B.
        /// </summary>
        /// <param name="start">Vertex at the start</param>
        /// <param name="end">Vertex at the end</param>
        /// <returns>true, if this edge runs from A to B, false if this edge has no pair, or does not connect the given vertices</returns>
        [Pure]
        public bool Connects(Vertex<TV, TE, TF> start, Vertex<TV, TE, TF> end)
        {
            return EndVertex.Equals(end) && StartVertex.Equals(start);
        }

        [Pure]
        public bool ConnectsTo(Vertex<TV, TE, TF> vertex)
        {
            if (vertex == null)
                return false;

            return EndVertex.Equals(vertex) || StartVertex.Equals(vertex);
        }

        [Pure]
        public override bool Equals(object obj)
        {
            var a = obj as HalfEdge<TV, TE, TF>;
            if (a != null)
                return Equals(a);

            return ReferenceEquals(this, obj);
        }

        [Pure]
        public bool Equals(HalfEdge<TV, TE, TF> e)
        {
            if (e == null)
                return false;

            Vertex<TV, TE, TF> a;
            Vertex<TV, TE, TF> b;
            GetVertices(out a, out b);

            Vertex<TV, TE, TF> x;
            Vertex<TV, TE, TF> y;
            e.GetVertices(out x, out y);

            return a.Equals(x) && b.Equals(y);
        }

        [Pure]
        public override int GetHashCode()
        {
            return 31 + (IsPrimaryEdge ? 17 : -29)
                 + 11 * EndVertex.GetHashCode();
        }

        [Pure]
        public override string ToString()
        {
            return Pair.EndVertex + "=>" + EndVertex;
        }

        #region Pathfinding IEdge
        Vertex<TV, TE, TF> IEdge<Vertex<TV, TE, TF>, HalfEdge<TV, TE, TF>>.Start
        {
            get
            {
                return Pair.EndVertex;
            }
        }

        Vertex<TV, TE, TF> IEdge<Vertex<TV, TE, TF>, HalfEdge<TV, TE, TF>>.End
        {
            get { return EndVertex; }
        }

        float IEdge<Vertex<TV, TE, TF>, HalfEdge<TV, TE, TF>>.TraversalCost
        {
            get
            {
                return (EndVertex.Position - Pair.EndVertex.Position).Length();
            }
        }

        IBaseVertex IBaseEdge.End
        {
            get { return EndVertex; }
        }

        IBaseVertex IBaseEdge.Start
        {
            get { return StartVertex; }
        }

        float IBaseEdge.TraversalCost
        {
            get { return (this as IEdge<Vertex<TV, TE, TF>, HalfEdge<TV, TE, TF>>).TraversalCost; }
        }

        public LineSegment2 Segment
        {
            get { return new LineSegment2(Pair.EndVertex.Position, EndVertex.Position); }
        }
        #endregion
    }
}
