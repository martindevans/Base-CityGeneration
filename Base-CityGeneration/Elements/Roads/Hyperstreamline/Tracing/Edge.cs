using System;
using System.Diagnostics.Contracts;
using System.Numerics;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class Edge
    {
        private readonly Vertex _a;
        public Vertex A
        {
            get
            {
                Contract.Ensures(Contract.Result<Vertex>() != null);
                return _a;
            }
        }

        private readonly Vertex _b;
        public Vertex B
        {
            get
            {
                Contract.Ensures(Contract.Result<Vertex>() != null);
                return _b;
            }
        }

        private readonly Vector2 _direction;
        public Vector2 Direction { get { return _direction; } }

        private readonly Streamline _streamline;
        public Streamline Streamline
        {
            get
            {
                Contract.Ensures(Contract.Result<Streamline>() != null);
                return _streamline;
            }
        }

        private Edge(Streamline stream, Vertex a, Vertex b)
        {
            Contract.Requires(stream != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);

            _a = a;
            _b = b;
            _streamline = stream;
            _direction = Vector2.Normalize(b.Position - a.Position);
        }

        /// <summary>
        /// Replace this edge with two new ones A -> Mid -> B
        /// </summary>
        /// <param name="mid"></param>
        /// <param name="aMid"></param>
        /// <param name="midB"></param>
        public Vertex Split(Vertex mid, out Edge aMid, out Edge midB)
        {
            Contract.Requires(mid != null);
            Contract.Ensures(ReferenceEquals(Contract.Result<Vertex>(), mid));
            Contract.Ensures(Contract.Result<Vertex>() != null);

            if (!_streamline.Add(mid))
                throw new InvalidOperationException("Invalid Split Operation: Streamline already contains mid vertex");

            if (!A.Remove(this))
                throw new InvalidOperationException("Invalid Split Operation: Vertex does not contain edge");
            if (!B.Remove(this))
                throw new InvalidOperationException("Invalid Split Operation: Vertex does not contain edge");

            aMid = Create(Streamline, A, mid);
            midB = Create(Streamline, mid, B);

            return mid;
        }

        public static Edge Create(Streamline stream, Vertex a, Vertex b)
        {
            Contract.Requires(stream != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Requires(!a.Equals(b), "Cannot create edge from a vertex to itself");
            Contract.Ensures(Contract.Result<Edge>() != null);

            var e = new Edge(stream, a, b);
            a.Add(e);
            b.Add(e);

            return e;
        }
    }
}
