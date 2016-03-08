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
        public Vector2 Position { get; private set; }

        /// <summary>
        /// The mesh this vertex is part of
        /// </summary>
        internal readonly Mesh<TVertexTag, THalfEdgeTag, TFaceTag> Mesh;

        internal IVertexBuilder Builder;

        private readonly ISet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> _edges = new HashSet<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>>();
        /// <summary>
        /// Edges emanating out from this edge
        /// </summary>
        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> Edges
        {
            get { return _edges; }
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

        internal Vertex(Mesh<TVertexTag, THalfEdgeTag, TFaceTag> m, Vector2 position)
        {
            Contract.Requires(m != null);

            Mesh = m;
            Position = position;
        }

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

        internal bool AddEdge(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edge)
        {
            Contract.Requires(edge != null);

            return _edges.Add(edge);
        }

        public bool DeleteEdge(HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag> edge)
        {
            Contract.Requires(edge != null);

            return _edges.Remove(edge);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var a = obj as Vertex<TVertexTag, THalfEdgeTag, TFaceTag>;
            if (a != null)
                return Equals(a);
            return ReferenceEquals(this, obj);
        }

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
