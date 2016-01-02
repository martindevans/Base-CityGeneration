using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Placeholder.AI.Pathfinding.Graph;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Vertex<TVertexTag, THalfEdgeTag, TFaceTag>
        :IVertex
    {
        public Vector2 Position { get; private set; }

        /// <summary>
        /// The mesh this vertex is part of
        /// </summary>
        internal readonly Mesh<TVertexTag, THalfEdgeTag, TFaceTag> Mesh;

        internal IVertexBuilder Builder;

        /// <summary>
        /// Edges emanating out from this edge
        /// </summary>
        public IEnumerable<HalfEdge<TVertexTag, THalfEdgeTag, TFaceTag>> Edges
        {
            get { return Mesh.EdgesFromVertex(this); }
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
    }
}
