using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Placeholder.AI.Pathfinding.Graph;

namespace Base_CityGeneration.Datastructures.HalfEdge
{
    public class Vertex
        :IVertex
    {
        public Vector2 Position { get; private set; }

        /// <summary>
        /// The mesh this vertex is part of
        /// </summary>
        internal readonly Mesh Mesh;

        internal IVertexBuilder Builder;

        /// <summary>
        /// Edges emanating out from this edge
        /// </summary>
        public IEnumerable<HalfEdge> Edges
        {
            get { return Mesh.EdgesFromVertex(this); }
        }

        internal Vertex(Mesh m, Vector2 position)
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
            if (obj is Vertex)
                return Equals(obj as Vertex);
            return ReferenceEquals(this, obj);
        }

        public bool Equals(Vertex other)
        {
            return other.Position == Position;
        }
    }
}
