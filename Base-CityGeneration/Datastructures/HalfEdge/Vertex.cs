using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Placeholder.AI.Pathfinding.Graph;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    /// <summary>
    /// A vertex in a half edge graph
    /// </summary>
    /// <typeparam name="TV">Type of additional data associated with vertices</typeparam>
    /// <typeparam name="TE">Type of additional data associated with half edges</typeparam>
    /// <typeparam name="TF">Type of additional data associated with faces</typeparam>
    public class Vertex<TV, TE, TF>
        : BaseTagged<TV, IVertexTag<TV, TE, TF>, Vertex<TV, TE, TF>>, IVertex
    {
        #region fields and properties
        public Vector2 Position { get; private set; }

        private readonly Mesh<TV, TE, TF> _mesh;
        /// <summary>
        /// The mesh this vertex is part of
        /// </summary>
        public Mesh<TV, TE, TF> Mesh
        {
            get
            {
                Contract.Ensures(Contract.Result<Mesh<TV, TE, TF>>() != null);
                return _mesh;
            }
        }

        private readonly ISet<HalfEdge<TV, TE, TF>> _edges = new HashSet<HalfEdge<TV, TE, TF>>();
        /// <summary>
        /// Edges emanating out from this edge
        /// </summary>
        public IEnumerable<HalfEdge<TV, TE, TF>> Edges
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<HalfEdge<TV, TE, TF>>>() != null);
                return _edges;
            }
        }

        public int EdgeCount
        {
            get { return _edges.Count; }
        }

        public IEnumerable<Face<TV, TE, TF>> Faces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Face<TV, TE, TF>>>() != null);

                return (from edge in Edges
                       let face = edge.Face
                       where face != null
                       select face).Distinct();
            }
        }
        #endregion

        #region constructor
        internal Vertex(Mesh<TV, TE, TF> m, Vector2 position)
        {
            Contract.Requires(m != null);

            Position = position;
            _mesh = m;
        }
        #endregion

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_mesh != null);
        }

        public override string ToString()
        {
            return "Vertex@" + Position;
        }

        IEnumerable<IEdge> IVertex.OutwardEdges
        {
            get
            {
                return Edges;
            }
        }

        public bool IsDeleted { get; internal set; }

        internal bool AddEdge(HalfEdge<TV, TE, TF> edge)
        {
            Contract.Requires(edge != null);
            Contract.Requires(edge.StartVertex.Equals(this));

            return _edges.Add(edge);
        }

        public bool DeleteEdge(HalfEdge<TV, TE, TF> edge)
        {
            Contract.Requires(edge != null);

            return _edges.Remove(edge);
        }

        [Pure]
        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        [Pure]
        public override bool Equals(object obj)
        {
            var a = obj as Vertex<TV, TE, TF>;
            if (a != null)
                return Equals(a);
            return ReferenceEquals(this, obj);
        }

        [Pure]
        public bool Equals(Vertex<TV, TE, TF> other)
        {
            return other != null && other.Position == Position;
        }

        internal void Transform(Func<Vector2, Vector2> transform)
        {
            Contract.Requires(transform != null);

            Position = transform(Position);
        }
    }
}
