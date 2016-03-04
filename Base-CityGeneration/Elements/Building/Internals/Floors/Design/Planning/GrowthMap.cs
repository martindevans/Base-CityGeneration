using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using HandyCollections.Geometry;
using HandyCollections.Heap;
using Myre.Collections;
using PrimitiveSvgBuilder;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using Vector2 = System.Numerics.Vector2;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    /// <summary>
    /// Given an outline and a set of seed points (on the outline) grow lines inwards to fill the space
    /// </summary>
    internal class GrowthMap
    {
        private readonly IReadOnlyList<Vector2> _outline;

        private readonly Func<double> _random;
        private readonly INamedDataCollection _metadata;

        private readonly IValueGenerator _seedDistance;
        private readonly IValueGenerator _parallelLengthMultiplier;
        private readonly IValueGenerator _parallelCheckDistance;
        private readonly IValueGenerator _cosineParallelAngleThreshold;

        private readonly Quadtree<Edge> _edges;
        private readonly MinHeap<Seed> _seeds = new MinHeap<Seed>();

        //todo: remove this!
        private readonly SvgBuilder _builder = new SvgBuilder(10);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outline">Polygon outline of this map (clockwise wound, potentially concave)</param>
        /// <param name="seedDistance">Distance between seeds placed on walls</param>
        /// <param name="random">PRNG (0-1)</param>
        /// <param name="metadata">Metadata used in random generation</param>
        public GrowthMap(IReadOnlyList<Vector2> outline, IValueGenerator seedDistance, Func<double> random, INamedDataCollection metadata, IValueGenerator parallelLengthMultiplier, IValueGenerator parallelCheckDistance, IValueGenerator parallelAngleThreshold)
        {
            Contract.Requires(outline != null);
            Contract.Requires(seedDistance != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(outline.IsClockwise());

            _outline = outline;
            _seedDistance = seedDistance;
            _random = random;
            _metadata = metadata;

            _parallelLengthMultiplier = parallelLengthMultiplier;
            _parallelCheckDistance = parallelCheckDistance;
            _cosineParallelAngleThreshold = parallelAngleThreshold.Transform(a => (float)Math.Cos(a));

            _edges = new Quadtree<Edge>(BoundingRectangle.CreateFromPoints(outline).Inflate(0.2f), 10);
        }

        public void Grow()
        {
            CreateInitialSeeds();

            //Pull seeds out of heap and grow them
            //todo: remove temp limit
            int count = 0;
            while (_seeds.Count > 0 && count++ < 1000)
            {
                var seed = _seeds.RemoveMin();

                GrowSeed(seed);
            }

            //todo: remove temp visualisation code
            foreach (var edge in _edges.Intersects(BoundingRectangle.CreateFromPoints(_outline).Inflate(0.2f)))
                _builder.Line(edge.A.Position, edge.B.Position, 1, "blue");
            Console.WriteLine(_builder.ToString());

            throw new NotImplementedException();
        }

        private void GrowSeed(Seed seed)
        {
            //Decide how far we're going to grow this seed
            var length = _seedDistance.SelectFloatValue(_random, _metadata);
            if (length < 0)
                throw new InvalidOperationException("Seed distance must be > 0");

            //Find the first edge which is parallel with this one
            var firstParallel = FindFirstParallelEdge(
                seed,
                length * _parallelLengthMultiplier.SelectFloatValue(_random, _metadata),
                _parallelCheckDistance.SelectFloatValue(_random, _metadata),
                _cosineParallelAngleThreshold.SelectFloatValue(_random, _metadata)
            );

            //Check for intersections with other edges
            var firstIntersection = FindFirstIntersection(seed, length + _seedDistance.MinValue );

            //Reject seeds with parallel edges in certain circumstances
            if (firstParallel.HasValue)
            {
                //There's a parallel edge and we don't intersect anything, so just ignore this seed altogether
                if (!firstIntersection.HasValue)
                    return;

                //There's a parallel edge and the first intersection if *after* the parallelism starts, so ignore this seed altogether
                if (firstIntersection.Value.Value.DistanceAlongB > firstParallel.Value.Value)
                    return;
            }


            if (firstIntersection.HasValue)
            {
                var intersectVert = CreateVertex(firstIntersection.Value.Value.Position);

                //Split the edge we've hit
                Edge ab, bc;
                SplitEdge(firstIntersection.Value.Key, intersectVert, out ab, out bc);

                //Create an edge to this intersection point
                InsertEdge(new Edge(seed.Origin, intersectVert));

                ////If this is not an external wall we can create a seed continuing forward
                //if (!intersection.Value.Key.External && _random.RandomBoolean())
                //{
                //    //New wall will be perpendicular to the wall we've hit...
                //    var direction = intersection.Value.Key.Segment.Line.Direction.Perpendicular();

                //    //...but which perpendicular?
                //    var dotRight = Vector2.Dot(direction, seed.Direction);
                //    var dotLeft = Vector2.Dot(-direction, seed.Direction);
                //    if (dotLeft > dotRight)
                //        direction *= -1;

                //    //create new seed
                //    var wallLength = Vector2.Distance(seed.Origin.Position, intersectVert.Position);
                //    CreateSeed(intersectVert, direction, seed.T + wallLength);
                //}
            }
            else
            {
                //Create edge along this distance
                var end = CreateVertex(seed.Origin.Position + seed.Direction * length);
                InsertEdge(new Edge(seed.Origin, end));

                // Choose which directions to grow in (LF, LR, FR, LFR) we're going to do
                var newWalls = _random.RandomInteger(0, 3);

                //Put some seeds at the end (right, left and straight on)
                if (newWalls != 0)
                    CreateSeed(end, seed.Direction.Perpendicular(), seed.T + length);
                if (newWalls != 2)
                    CreateSeed(end, -seed.Direction.Perpendicular(), seed.T + length);
                if (newWalls != 1)
                    CreateSeed(end, seed.Direction, seed.T + length);
            }
        }

        private KeyValuePair<Edge, float>? FindFirstParallelEdge(Seed seed, float length, float distance, float parallelThreshold)
        {
            var start = seed.Origin.Position;
            var end = seed.Origin.Position + seed.Direction * length;
            var segment = new LineSegment2(start, end);

            //Calculate the expanded bounds to query. This is as wide as the parallel check distance
            var p = seed.Direction.Perpendicular() * distance / 2;
            var a = start + p;
            var b = start - p;
            var c = end + p;
            var d = end - p;
            var queryBounds = new BoundingRectangle(
                Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d)),
                Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d))
            );

            //now get all lines which intersect this bounds and check them for parallelism
            var candidates = _edges.Intersects(queryBounds);

            KeyValuePair<Edge, float>? firstParallel = null;
            foreach (var candidate in candidates)
            {
                var dirCandidate = candidate.Segment.Line.Direction;
                var dir = segment.Line.Direction;

                //Dot product directions of lines to check parallelism (compare with threshold)
                var dot = Math.Abs(Vector2.Dot(dir, dirCandidate));
                if (dot > parallelThreshold)
                {
                    //Our query bounds were larger than the actual area we wanted to query (because we're limited to axis aligned bounds)
                    //Check that this line enters the smaller OABB area
                    //We'll do this check by checking if the line segment intersects any of the four OABB segments (AB, BC, CD, DA)

                    if (new LineSegment2(a, b).Intersects(candidate.Segment).HasValue
                        || new LineSegment2(b, c).Intersects(candidate.Segment).HasValue
                        || new LineSegment2(c, d).Intersects(candidate.Segment).HasValue
                        || new LineSegment2(d, a).Intersects(candidate.Segment).HasValue)
                    {
                        //check how far along this segment the parallelism starts

                        var startDist = segment.ClosestPointDistanceAlongSegment(candidate.A.Position);
                        var endDist = segment.ClosestPointDistanceAlongSegment(candidate.B.Position);
                        var minDist = Math.Min(startDist, endDist);

                        if (firstParallel == null || minDist < firstParallel.Value.Value)
                            firstParallel = new KeyValuePair<Edge, float>(candidate, minDist);
                    }
                }

            }

            return firstParallel;
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
                var b = vertices[i];
                var c = vertices[(i + 1) % vertices.Length];

                //Create a series of edges between these two vertices (not just one edge, because we drop seeds along the line as we go)
                CreateOutlineEdge(b, c);

                //We want to measure the internal angle at vertex "b", for that we need the previous vertex (which we'll call "a")
                var a = vertices[(i + vertices.Length - 1) % vertices.Length];

                //Calculate the inner angle between these vectors (not always clockwise!)
                var ab = Vector2.Normalize(b.Position - a.Position);
                var bc = Vector2.Normalize(c.Position - b.Position);
                var dot = Vector2.Dot(bc, -ab);
                var det = bc.Cross(-ab);
                var angle = (float)(Math.Atan2(det, dot) % (Math.PI * 2));
                angle = det < 0 ? angle * -1 : (float)Math.PI * 2 - angle;
                
                if (angle < Math.PI * 0.51)
                {
                    //0 -> 90 degrees
                    //Do nothing!
                }
                else if (angle <= Math.PI * 1.01)
                {
                    //90 -> 180
                    if (_random.RandomBoolean())
                        PerpendicularSeed(b, ab);
                    else
                        PerpendicularSeed(b, bc);
                }
                else if (angle <= Math.PI * 1.51)
                {
                    //180 -> 270
                    //if (_random.RandomBoolean())
                    //    BisectorSeed(b, -ab, -bc);  //Negated, to ensure bisection is on the correct side (angle is > 180, so by default bisection would be on wrong side)
                    //else
                    {
                        PerpendicularSeed(b, ab);
                        PerpendicularSeed(b, bc);
                    }
                }
                else
                {
                    //270 -> 360
                    //BisectorSeed(b, -ab, -bc);  //Negated, to ensure bisection is on the correct side (angle is > 180, so by default bisection would be on wrong side)
                    PerpendicularSeed(b, ab);
                    PerpendicularSeed(b, bc);
                }
            }
        }

        private void BisectorSeed(Vertex b, Vector2 ab, Vector2 bc)
        {
            //Average of -in and out is the bisector
            var bi = Vector2.Normalize(-ab * 0.5f + bc * 0.5f);

            CreateSeed(b, bi, (float)_random());
        }

        private void PerpendicularSeed(Vertex a, Vector2 ab)
        {
            CreateSeed(a, ab.Perpendicular(), (float)_random());
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
                CreateSeed(v, direction.Perpendicular(), (float)_random());
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
