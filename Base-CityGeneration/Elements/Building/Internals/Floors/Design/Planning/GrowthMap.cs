using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using HandyCollections.Heap;
using Myre.Collections;
using PrimitiveSvgBuilder;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using Vector2 = System.Numerics.Vector2;

using MVertex = Base_CityGeneration.Datastructures.HalfEdge.Vertex<
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag
>;
using MHEdge = Base_CityGeneration.Datastructures.HalfEdge.HalfEdge<
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag
>;

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
        private readonly IValueGenerator _seedChance;
        private readonly IValueGenerator _parallelLengthMultiplier;
        private readonly IValueGenerator _parallelCheckWidth;
        private readonly IValueGenerator _cosineParallelAngleThreshold;
        private readonly IValueGenerator _intersectionContinuationChance;

        private readonly Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> _mesh; 
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
        /// <param name="parallelLengthMultiplier"></param>
        /// <param name="parallelCheckWidth"></param>
        /// <param name="seedChance"></param>
        /// <param name="parallelAngleThreshold"></param>
        public GrowthMap(IReadOnlyList<Vector2> outline, IValueGenerator seedDistance, Func<double> random, INamedDataCollection metadata, IValueGenerator parallelLengthMultiplier, IValueGenerator parallelCheckWidth, IValueGenerator parallelAngleThreshold, IValueGenerator intersectionContinuationChance, IValueGenerator seedChance)
        {
            Contract.Requires(outline != null);
            Contract.Requires(seedDistance != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(outline.IsClockwise());
            Contract.Requires(parallelLengthMultiplier != null);
            Contract.Requires(parallelCheckWidth != null);
            Contract.Requires(seedChance != null);
            Contract.Requires(parallelAngleThreshold != null);

            _outline = outline;
            _seedDistance = seedDistance;
            _seedChance = seedChance;
            _random = random;
            _metadata = metadata;

            _parallelLengthMultiplier = parallelLengthMultiplier;
            _parallelCheckWidth = parallelCheckWidth;
            _cosineParallelAngleThreshold = parallelAngleThreshold.Transform(a => (float)Math.Cos(a));
            _intersectionContinuationChance = intersectionContinuationChance;

            var bounds = BoundingRectangle.CreateFromPoints(outline).Inflate(0.2f);
            _mesh = new Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>(bounds);
        }

        public Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> Grow()
        {
            //Create initial seeds along the outline of the building
            CreateInitialSeeds();

            //Pull seeds out of heap and grow them (eventually we will run out of valid seeds and exit this loop)
            while (_seeds.Count > 0)
                GrowSeed(_seeds.RemoveMin());

            //Sometimes we create edges which lead to a vertex and then nothing else, remove all these dead end edges
            CleanupDeadEnds();

            //Put faces in between the walls we've created
            _mesh.CreateImplicitFaces();

            Stopwatch w = new Stopwatch();
            w.Start();
            _mesh.SimplifyFaces();
            Console.WriteLine(w.ElapsedMilliseconds + "ms");

            //todo: remove temp visualisation code
            //foreach (var edge in _mesh.HalfEdges.Where(a => a.IsPrimaryEdge))
            //    _builder.Outline(edge.Bounds.GetCorners(), "red");
            foreach (var face in _mesh.Faces)
                _builder.Outline(face.Vertices.Select(a => a.Position).Shrink(0.1f).ToArray(), stroke: "none", fill: "red");
            //foreach (var edge in _mesh.HalfEdges.Where(a => a.IsPrimaryEdge))
            //    _builder.Line(edge.StartVertex.Position, edge.EndVertex.Position, 1, "blue");
            foreach (var vertex in _mesh.Vertices)
                _builder.Circle(vertex.Position, 0.15f, "green");
            Console.WriteLine(_builder.ToString());

            return ConvertToMesh();
        }

        #region growth
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
                _parallelCheckWidth.SelectFloatValue(_random, _metadata),
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
                var intersectVert = _mesh.GetOrConstructVertex(firstIntersection.Value.Value.Position);
                if (intersectVert.Equals(seed.Origin))
                    return;

                //If the edge doesn't contain this vertex already then split the edge we've hit
                if (!firstIntersection.Value.Key.ConnectsTo(intersectVert))
                {
                    MHEdge ab, bc;
                    _mesh.Split(firstIntersection.Value.Key, intersectVert, out ab, out bc);

                    var t = firstIntersection.Value.Key.Tag ?? firstIntersection.Value.Key.Pair.Tag;
                    ab.Tag = new FloorplanHalfEdgeTag(t.IsExternal);
                    bc.Tag = new FloorplanHalfEdgeTag(t.IsExternal);
                }

                //Create an edge to this intersection point
                _mesh.GetOrConstructHalfEdge(seed.Origin, intersectVert).Tag = new FloorplanHalfEdgeTag(false);

                //If this is not an external wall we can create a seed continuing forward
                var tag = firstIntersection.Value.Key.Tag ?? firstIntersection.Value.Key.Pair.Tag;
                var continuationChance = _intersectionContinuationChance.SelectFloatValue(_random, _metadata);
                if (!tag.IsExternal && _random.RandomBoolean(1 - continuationChance))
                {
                    //New wall will be perpendicular to the wall we've hit...
                    var direction = firstIntersection.Value.Key.Segment.Line.Direction.Perpendicular();

                    //...but which perpendicular?
                    var dotRight = Vector2.Dot(direction, seed.Direction);
                    var dotLeft = Vector2.Dot(-direction, seed.Direction);
                    if (dotLeft > dotRight)
                        direction *= -1;

                    //create new seed
                    var wallLength = Vector2.Distance(seed.Origin.Position, intersectVert.Position);
                    CreateSeed(intersectVert, direction, seed.T + wallLength);
                }
            }
            else
            {
                //Create edge along this distance
                var end = _mesh.GetOrConstructVertex(seed.Origin.Position + seed.Direction * length);
                _mesh.GetOrConstructHalfEdge(seed.Origin, end).Tag = new FloorplanHalfEdgeTag(false);

                float seedChance = _seedChance.SelectFloatValue(_random, _metadata);
                if (_random.RandomBoolean(seedChance))
                {
                    CreateSeed(end, seed.Direction, seed.T + length);
                }
                else
                {
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
        }

        private KeyValuePair<MHEdge, float>? FindFirstParallelEdge(Seed seed, float length, float distance, float parallelThreshold)
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
            var candidates = _mesh.FindEdges(queryBounds);

            KeyValuePair<MHEdge, float>? firstParallel = null;
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

                        var startDist = segment.ClosestPointDistanceAlongSegment(candidate.StartVertex.Position);
                        var endDist = segment.ClosestPointDistanceAlongSegment(candidate.EndVertex.Position);
                        var minDist = Math.Min(startDist, endDist);

                        if (firstParallel == null || minDist < firstParallel.Value.Value)
                            firstParallel = new KeyValuePair<MHEdge, float>(candidate, minDist);
                    }
                }

            }

            return firstParallel;
        }

        private KeyValuePair<MHEdge, LinesIntersection2>? FindFirstIntersection(Seed seed, float length)
        {
            Contract.Requires(seed != null);
            Contract.Ensures(!Contract.Result<KeyValuePair<MHEdge, LinesIntersection2>?>().HasValue || Contract.Result<KeyValuePair<MHEdge, LinesIntersection2>?>().Value.Key != null);

            //Create the bounds of this new line (inflated slightly, just in case it's perfectly axis aligned)
            var a = seed.Origin.Position;
            var b = seed.Origin.Position + seed.Direction * length;
            var bounds = new BoundingRectangle(Vector2.Min(a, b), Vector2.Max(a, b)).Inflate(0.2f);
            var segment = new LineSegment2(a, b);

            //Find all edges which intersect this bounds, then test them one by one for intersection
            var results = _mesh
                .FindEdges(bounds)
                .Select(e => new KeyValuePair<MHEdge, LinesIntersection2?>(e, e.Segment.Intersects(segment)))
                .Where(i => i.Value.HasValue)
                .Select(i => new KeyValuePair<MHEdge, LinesIntersection2>(i.Key, i.Value.Value));

            //Find the first intersection from all the results
            KeyValuePair<MHEdge, LinesIntersection2>? result = null;
            foreach (var candidate in results)
            {
                if (result == null || candidate.Value.DistanceAlongB < result.Value.Value.DistanceAlongB)
                    result = candidate;
            }
            return result;
        }
        #endregion

        #region cleanup
        private void CleanupDeadEnds()
        {
            //Keep running this until we reach a fixpoint (i.e. nothing changed)
            Func<IEnumerable<MVertex>, bool> cleanup = verts =>
            {
                //Try to find a vertex to remove
                var vertex = verts.FirstOrDefault(v => v.EdgeCount <= 1);
                if (vertex == null)
                    return false;

                //Delete it and return a value indicating we changed something
                _mesh.Delete(vertex);
                return true;

            };

            //At this point you're thinking to yourself "this could be optimized loads! Why are we constructing a Func and then only doing one thing at a time in each iteration?"
            //You're wrong - it's always 12ms runtime!
            //Failed optimizing attempts:
            // - while (true) { modify list of all vertices }
            // - fixpoint(() => { do several vertices in each iteration })
            // - Define cleanup as an instance method and cast to Func for fixpoint
            cleanup.Fixpoint(_mesh.Vertices);
        }

        private Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> ConvertToMesh()
        {
            return _mesh;
        }
        #endregion

        #region initialisation
        private void CreateInitialSeeds()
        {
            var vertices = _outline.Select(_mesh.GetOrConstructVertex).ToArray();

            // Create the outer edges of the floor
            for (var i = 0; i < vertices.Length; i++)
            {
                //Start and end vertex of this wall
                var b = vertices[i];
                var c = vertices[(i + 1) % vertices.Length];

                //Create a series of edges between these two vertices (not just one edge, because we drop seeds along the line as we go)
                CreateExternalEdge(b, c);

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

        private void PerpendicularSeed(MVertex a, Vector2 ab)
        {
            CreateSeed(a, ab.Perpendicular(), (float)_random());
        }

        /// <summary>
        /// Create a series of edges from A to B, dropping seeds along the way (all seeds point to the right)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private void CreateExternalEdge(MVertex a, MVertex b)
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
                var v = _mesh.GetOrConstructVertex(a.Position + direction * used);

                //Create edge up to this vertex and a seed (pointing to the right, which is inwards assuming the outline is clockwise wound)
                CreateSeed(v, direction.Perpendicular(), (float)_random());
                _mesh.GetOrConstructHalfEdge(current, v).Tag = new FloorplanHalfEdgeTag(true);

                current = v;
            }

            //Finish off the end of the edge
            _mesh.GetOrConstructHalfEdge(current, b).Tag = new FloorplanHalfEdgeTag(true);
        }
        #endregion

        #region seeds
        private void CreateSeed(MVertex start, Vector2 direction, float t)
        {
            //This could be written as `direction.LengthSquared().TolerantEquals(1, 0.001f)` However, LengthSquared() is not marked as [Pure]
            Contract.Requires((direction.X * direction.X + direction.Y * direction.Y).TolerantEquals(1, 0.001f));

            _seeds.Add(new Seed(start, direction, t + (float)_random() * 0.1f));
        }
        #endregion

        #region helper classes
        private class Seed
            : IComparable<Seed>
        {
            public MVertex Origin { get; private set; }
            public Vector2 Direction { get; private set; }
            public float T { get; private set; }

            public Seed(MVertex origin, Vector2 direction, float t)
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
        #endregion
    }
}
