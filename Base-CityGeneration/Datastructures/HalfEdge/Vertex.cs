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
    /// <typeparam name="TVertexTag">Type of additional data associated with vertices</typeparam>
    /// <typeparam name="THalfEdgeTag">Type of additional data associated with half edges</typeparam>
    /// <typeparam name="TFaceTag">Type of additional data associated with faces</typeparam>
    public class Vertex<TVertexTag, THalfEdgeTag, TFaceTag>
        :IVertex
    {
        #region fields and properties
        public Vector2 Position { get; private set; }

        private readonly Mesh<TVertexTag, THalfEdgeTag, TFaceTag> _mesh;
        /// <summary>
        /// The mesh this vertex is part of
        /// </summary>
        public Mesh<TVertexTag, THalfEdgeTag, TFaceTag> Mesh
        {
            get { return _mesh; }
        }

        internal IVertexBuilder Builder;

        private readonly ISet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> _edges = new HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>();
        /// <summary>
        /// Edges emanating out from this edge
        /// </summary>
        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> Edges
        {
            get { return _edges; }
        }

        public int EdgeCount
        {
            get { return _edges.Count; }
        }

        public IEnumerable<Face<TVertexTag, THalfEdgeTag, TFaceTag>> Faces
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Face<TVertexTag, THalfEdgeTag, TFaceTag>>>() != null);

                return (from edge in Edges
                       let face = edge.Face
                       where face != null
                       select face).Distinct();
            }
        }
        #endregion

        #region constructor
        internal Vertex(Mesh<TVertexTag, THalfEdgeTag, TFaceTag> m, Vector2 position)
        {
            Contract.Requires(m != null);

            Position = position;
            _mesh = m;
        }
        #endregion

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(Mesh != null);
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

        internal bool AddEdge(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edge)
        {
            Contract.Requires(edge != null);
            Contract.Requires(edge.StartVertex.Equals(this));

            return _edges.Add(edge);
        }

        public bool DeleteEdge(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edge)
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
            var a = obj as Vertex<TVertexTag, THalfEdgeTag, TFaceTag>;
            if (a != null)
                return Equals(a);
            return ReferenceEquals(this, obj);
        }

        [Pure]
        public bool Equals(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> other)
        {
            return other != null && other.Position == Position;
        }

        internal void Transform(Func<Vector2, Vector2> transform)
        {
            Position = transform(Position);
        }

        
    }
}
