using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class Vertex
    {
        internal readonly Vector2 PositionField;
        public Vector2 Position { get { return PositionField; } }

        private readonly List<Edge> _edges = new List<Edge>();
        public IEnumerable<Edge> Edges { get { return _edges; } }

        public int EdgeCount { get { return _edges.Count; } }

        private static int _nextHash = int.MinValue;
        private readonly int _hash;

        public Vertex(Vector2 position)
        {
            PositionField = position;

            _hash = Interlocked.Increment(ref _nextHash);
        }

        internal void Add(Edge edge)
        {
            _edges.Add(edge);
        }

        internal bool Remove(Edge edge)
        {
            return _edges.Remove(edge);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(obj, this);
        }
    }
}
