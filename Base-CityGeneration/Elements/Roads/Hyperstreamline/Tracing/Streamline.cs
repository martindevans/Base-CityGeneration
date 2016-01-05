using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class Streamline
    {
        private readonly HashSet<Vertex> _vertices = new HashSet<Vertex>();
        public IEnumerable<Vertex> Vertices
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<Vertex>>() != null);
                return _vertices;
            }
        }

        public int Count
        {
            get
            {
                return _vertices.Count;
            }
        }

        public Vertex First { get; private set; }

        public Vertex Last { get; private set; }

        public uint Width { get; set; }

        public Region Region { get; set; }

        public Streamline(Vertex start)
        {
            First = start;
            Last = start;

            _vertices.Add(start);
        }

        internal bool Add(Vertex vertex)
        {
            Contract.Requires(vertex != null);

            return _vertices.Add(vertex);
        }

        internal bool Remove(Vertex vertex)
        {
            Contract.Requires(vertex != null);

            return _vertices.Remove(vertex);
        }

        public bool Contains(Vertex vertex)
        {
            Contract.Requires(vertex != null);

            return _vertices.Contains(vertex);
        }

        internal Edge Extend(Vertex endVertex)
        {
            Contract.Requires(endVertex != null);

            if (!First.Equals(endVertex) && !Add(endVertex))
                throw new ArgumentException("Cannot extend: Streamline already contains vertex", "endVertex");

            //Check that we are not connecting two vertices which are already connected
            var a = Last;
            var b = endVertex;
            if (a.Edges.Any(e => e.A.Equals(a) && e.B.Equals(b) || e.A.Equals(b) && e.B.Equals(a)))
                return null;

            //Extend
            Last = b;
            return Edge.Create(this, a, b);
        }
    }
}
