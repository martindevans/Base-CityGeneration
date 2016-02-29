using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using HandyCollections.Geometry;
using HandyCollections.Heap;
using Myre.Collections;
using PrimitiveSvgBuilder;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    /// <summary>
    /// Given an outline and a set of seed points (on the outline) grow lines inwards to fill the space
    /// </summary>
    internal class GrowthMap
    {
        private readonly IReadOnlyList<Vector2> _outline;
        private readonly IValueGenerator _seedDistance;
        private readonly float _innerAngleBisectChance;
        private readonly Func<double> _random;
        private readonly INamedDataCollection _metadata;

        private readonly Quadtree<Edge> _edges;
        private readonly MinHeap<Seed> _seeds = new MinHeap<Seed>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outline">Polygon outline of this map (clockwise wound, potentially concave)</param>
        /// <param name="seedDistance">Distance between seeds placed on walls</param>
        /// <param name="innerAngleBisectChance"></param>
        /// <param name="random">PRNG (0-1)</param>
        /// <param name="metadata">Metadata used in random generation</param>
        public GrowthMap(IReadOnlyList<Vector2> outline, IValueGenerator seedDistance, float innerAngleBisectChance, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(outline != null);
            Contract.Requires(seedDistance != null);
            Contract.Requires(innerAngleBisectChance >= 0 && innerAngleBisectChance <= 1);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            _outline = outline;
            _seedDistance = seedDistance;
            _innerAngleBisectChance = innerAngleBisectChance;
            _random = random;
            _metadata = metadata;

            _edges = new Quadtree<Edge>(BoundingRectangle.CreateFromPoints(outline).Inflate(0.2f), 10);
        }

        public void Grow()
        {
            CreateInitialSeeds();

            //todo: remove temp visualisation code
            var builder = new SvgBuilder(10);

            //Pull seeds out of heap and grow them
            int count = 0;
            while (_seeds.Count > 0 && count++ < 500)
            {
                var seed = _seeds.RemoveMin();

                var length = _seedDistance.SelectFloatValue(_random, _metadata);
                if (length < 0)
                    throw new InvalidOperationException("Seed distance must be > 0");

                var intersection = FindFirstIntersection(seed, length);

                if (intersection.HasValue)
                {
                    builder.Circle(intersection.Value.Value.Position, 0.25f, "hotpink");

                    var intersectVert = CreateVertex(intersection.Value.Value.Position);

                    //Split the edge we've hit
                    Edge ab, bc;
                    SplitEdge(intersection.Value.Key, intersectVert, out ab, out bc);

                    //Create an edge to this intersection point
                    InsertEdge(new Edge(seed.Origin, intersectVert));

                    //create new seed going straight on (unless this is en external wall)
                    if (!intersection.Value.Key.External && _random.RandomBoolean())
                        CreateSeed(intersectVert, seed.Direction, seed.T + length);
                }
                else
                {
                    //Create edge along this distance
                    var end = CreateVertex(seed.Origin.Position + seed.Direction * length);
                    InsertEdge(new Edge(seed.Origin, end));

                    var l = _random.RandomBoolean();
                    var r = _random.RandomBoolean(l ? 0.5f : 0.25f);
                    var f = _random.RandomBoolean(l || r ? 0.5f : 0);

                    //Put some seeds at the end (right, left and straight on)
                    if (r)
                        CreateSeed(end, seed.Direction.Perpendicular(), seed.T + length);
                    if (l)
                        CreateSeed(end, -seed.Direction.Perpendicular(), seed.T + length);
                    if (f)
                        CreateSeed(end, seed.Direction, seed.T + length);
                }
            }

            //todo: remove temp visualisation code
            foreach (var edge in _edges.Intersects(BoundingRectangle.CreateFromPoints(_outline).Inflate(0.2f)))
                builder.Line(edge.A.Position, edge.B.Position, 1, "blue");
            Console.WriteLine(builder.ToString());

            throw new NotImplementedException();
        }

        private KeyValuePair<Edge, LinesIntersection2>? FindFirstIntersection(Seed seed, float length)
        {
            Contract.Requires(seed != null);
            Contract.Ensures(!Contract.Result<KeyValuePair<Edge, LinesIntersection2>?>().HasValue || Contract.Result<KeyValuePair<Edge, LinesIntersection2>?>().Value.Key != null);

            //Create the bounds of this new line (inflated slightly, just in case it's perfectly axis aligned)
            var a = seed.Origin.Position;
            var b = seed.Origin.Position + seed.Direction * length;
            var bounds = new BoundingRectangle(Vector2.Min(a, b), Vector2.Max(a, b)).Inflate(0.2f);
            var segment = new LineSegment2(a, b);

            //Find all edges which intersect this bounds, then test them one by one for intersection
            var results = _edges
                .Intersects(bounds)
                .Select(e => new KeyValuePair<Edge, LinesIntersection2?>(e, e.Segment.Intersects(segment)))
                .Where(i => i.Value.HasValue)
                .Select(i => new KeyValuePair<Edge, LinesIntersection2>(i.Key, i.Value.Value));

            //Find the first intersection from all the results
            KeyValuePair<Edge, LinesIntersection2>? result = null;
            foreach (var candidate in results)
            {
                if (result == null || candidate.Value.DistanceAlongB < result.Value.Value.DistanceAlongB)
                    result = candidate;
            }
            return result;
        }

        #region initialisation
        private void CreateInitialSeeds()
        {
            var vertices = _outline.Select(CreateVertex).ToArray();

            // Create the outer edges of the floor
            for (var i = 0; i < vertices.Length; i++)
            {
                //Start and end vertex of this wall
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Length];

                //Create a series of edges between these two vertices (not just one edge, because we drop seeds along the line as we go)
                CreateOutlineEdge(a, b);

                ////todo: if vertex a is on an internal angle of >= 180 degrees, add it as a seed point
                //var z = vertices[(i + vertices.Length - 1) % vertices.Length];
                //var za = a.Position - z.Position;
                //var ab = b.Position - a.Position;
                //var internalAngle = za.Cross(ab);
                //if (internalAngle > 0)
                //{
                //    //Either perfectly bisect the angle, or choose one of the two walls and create a perpendicular wall
                //    if (_random() < _innerAngleBisectChance)
                //        seeds.Add(new Seed(a, -(za * 0.5f + ab * 0.5f)));
                //    else if (_random.RandomBoolean())
                //        seeds.Add(new Seed(a, za.Perpendicular()));
                //    else
                //        seeds.Add(new Seed(a, ab.Perpendicular()));
                //}
            }
        }

        /// <summary>
        /// Create a series of edges from A to B, dropping seeds along the way (all seeds point to the right)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private void CreateOutlineEdge(Vertex a, Vertex b)
        {
            //step along the gap between these vertices in steps of *seedDistance* placing vertices
            var direction = b.Position - a.Position;
            var totalLength = direction.Length();
            direction /= totalLength;
            var used = 0f;

            var current = a;
            while (true)
            {
                used += _seedDistance.SelectFloatValue(_random, _metadata);
                if (used >= totalLength || (totalLength - used) < _seedDistance.MinValue)
                    break;

                //Create the vertex at this point
                var v = CreateVertex(a.Position + direction * used);

                //Create edge up to this vertex and a seed (pointing to the right, which is inwards assuming the outline is clockwise wound)
                CreateSeed(v, direction.Perpendicular(), 0);
                InsertEdge(new Edge(current, v, true));

                current = v;
            }

            //Finish off the end of the edge
            InsertEdge(new Edge(current, b, true));
        }
        #endregion

        #region seeds
        private void CreateSeed(Vertex start, Vector2 direction, float t)
        {
            Contract.Requires(direction.LengthSquared().TolerantEquals(1, 0.001f));

            _seeds.Add(new Seed(start, direction, t + (float)_random() * 0.1f));
        }
        #endregion

        #region vertices
        private Vertex CreateVertex(Vector2 position)
        {
            //todo: remake this into GetOrCreate, with very careful consideration for how to do that exactly!
            return new Vertex(position);
        }
        #endregion

        #region edges
        private Edge InsertEdge(Edge edge, out BoundingRectangle bounds)
        {
            Contract.Requires(edge != null);

            //Create a bounding box for this edge
            //bounds are inflated slightly to ensure that if this edge is perfectly axis aligned we don't get a zero size box
            bounds = new BoundingRectangle(Vector2.Min(edge.A.Position, edge.B.Position), Vector2.Max(edge.A.Position, edge.B.Position)).Inflate(0.2f);

            //Insert this edge into the quadtree
            _edges.Insert(bounds, edge);

            return edge;
        }

        private Edge InsertEdge(Edge edge)
        {
            Contract.Requires(edge != null);

            BoundingRectangle b;
            return InsertEdge(edge, out b);
        }

        private void SplitEdge(Edge edge, Vertex mid, out Edge ab, out Edge bc)
        {
            Contract.Requires(edge != null);
            Contract.Requires(mid != null);
            Contract.Ensures(Contract.ValueAtReturn(out ab) != null);
            Contract.Ensures(Contract.ValueAtReturn(out bc) != null);

            //Delete the old edge...
            DeleteEdge(edge);

            //...replace two new edges
            ab = InsertEdge(new Edge(edge.A, mid, edge.External));
            bc = InsertEdge(new Edge(mid, edge.B, edge.External));
        }

        private void DeleteEdge(Edge edge)
        {
            Contract.Requires(edge != null);

            _edges.Remove(new BoundingRectangle(Vector2.Min(edge.A.Position, edge.B.Position), Vector2.Max(edge.A.Position, edge.B.Position)).Inflate(0.2f), edge);
        }
        #endregion

        #region helper classes
        private class Seed
            : IComparable<Seed>
        {
            public Vertex Origin { get; private set; }
            public Vector2 Direction { get; private set; }
            public float T { get; private set; }

            public Seed(Vertex origin, Vector2 direction, float t)
            {
                Origin = origin;
                Direction = direction;
                T = t;
            }

            public int CompareTo(Seed other)
            {
                return T.CompareTo(other.T);
            }
        }

        private class Edge
        {
            private readonly bool _external;
            public bool External
            {
                get { return _external; }
            }

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

            public LineSegment2 Segment { get { return new LineSegment2(A.Position, B.Position); } }

            public Edge(Vertex a, Vertex b, bool external = false)
            {
                Contract.Requires(a != null);
                Contract.Requires(b != null);
                Contract.Requires(!a.Equals(b), "Cannot create edge from a vertex to itself");

                _a = a;
                _b = b;
                _external = external;
            }
        }

        private class Vertex
        {
            private readonly Vector2 _position;
            public Vector2 Position { get { return _position; } }

            public Vertex(Vector2 position)
            {
                _position = position;
            }
        }
        #endregion
    }
}
