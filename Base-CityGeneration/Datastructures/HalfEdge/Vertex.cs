using System.Collections.Generic;
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
            Mesh = m;
            Position = position;
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
            if (obj is Vertex<TVertexTag, THalfEdgeTag, TFaceTag>)
                return Equals(obj as Vertex<TVertexTag, THalfEdgeTag, TFaceTag>);
            return ReferenceEquals(this, obj);
        }

        public bool Equals(Vertex<TVertexTag, THalfEdgeTag, TFaceTag> other)
        {
            return other.Position == Position;
        }
    }
}
