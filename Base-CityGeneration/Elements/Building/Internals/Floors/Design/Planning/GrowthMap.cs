using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using HandyCollections.Heap;
using JetBrains.Annotations;
using MathHelperRedux;
using Myre.Collections;
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
    public class WallGrowthParameters
    {
        private readonly IValueGenerator _seedDistance;
        private readonly IValueGenerator _seedChance;
        private readonly IValueGenerator _parallelLengthMultiplier;
        private readonly IValueGenerator _parallelCheckWidth;
        private readonly IValueGenerator _parallelAngleThreshold;
        private readonly IValueGenerator _intersectionContinuationChance;

        public IValueGenerator SeedDistance
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _seedDistance;
            }
        }

        public IValueGenerator SeedChance
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _seedChance;
            }
        }

        public IValueGenerator ParallelLengthMultiplier
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _parallelLengthMultiplier;
            }
        }

        public IValueGenerator ParallelCheckWidth
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _parallelCheckWidth;
            }
        }

        public IValueGenerator ParallelAngleThreshold
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _parallelAngleThreshold;
            }
        }

        public IValueGenerator IntersectionContinuationChance
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _intersectionContinuationChance;
            }
        }

        public WallGrowthParameters(IValueGenerator seedDistance, IValueGenerator seedChance, IValueGenerator parallelLengthMultiplier, IValueGenerator parallelCheckWidth, IValueGenerator parallelAngleThreshold, IValueGenerator intersectionContinuationChance)
        {
            Contract.Requires(seedDistance != null);
            Contract.Requires(seedChance != null);
            Contract.Requires(parallelLengthMultiplier != null);
            Contract.Requires(parallelCheckWidth != null);
            Contract.Requires(parallelAngleThreshold != null);
            Contract.Requires(intersectionContinuationChance != null);

            _seedDistance = seedDistance;
            _seedChance = seedChance;
            _parallelLengthMultiplier = parallelLengthMultiplier;
            _parallelCheckWidth = parallelCheckWidth;
            _parallelAngleThreshold = parallelAngleThreshold;
            _intersectionContinuationChance = intersectionContinuationChance;
        }

        internal class Container
            : IUnwrappable<WallGrowthParameters>
        {
            /// <summary>
            /// Parameters for parallelism checking
            /// </summary>
            public ParallelCheckParameters? ParallelCheck { get; [UsedImplicitly] set; }

            /// <summary>
            /// Distance between seeds along walls
            /// </summary>
            public object SeedSpacing { get; [UsedImplicitly] set; }

            /// <summary>
            /// Chance that a seed will be placed at a seed point (otherwise the wall will continue straight on)
            /// </summary>
            public object SeedChance { get; [UsedImplicitly] set; }

            /// <summary>
            /// When a wall hits another wall chance that the wall will continue out the other side
            /// </summary>
            public object IntersectionContinuationChance { get; [UsedImplicitly] set; }

            public WallGrowthParameters Unwrap()
            {
                Contract.Assume(SeedSpacing != null);
                Contract.Assume(SeedChance != null);

                //Get parallel parameters (or use defaults)
                var defaultParallel = new ParallelCheckParameters
                {
                    Length = 1.25f,
                    Width = 1,
                    Angle = 10
                };
                var parallelParams = ParallelCheck ?? defaultParallel;

                return new WallGrowthParameters(
                    IValueGeneratorContainer.FromObject(SeedSpacing, new NormallyDistributedValue(1.5f, 3, 4.5f, 0.5f)),
                    IValueGeneratorContainer.FromObject(SeedChance, 0.5f),
                    IValueGeneratorContainer.FromObject(parallelParams.Length, defaultParallel.Length),
                    IValueGeneratorContainer.FromObject(parallelParams.Width, defaultParallel.Width),
                    IValueGeneratorContainer.FromObject(parallelParams.Angle, defaultParallel.Angle).Transform(MathHelper.ToRadians),
                    IValueGeneratorContainer.FromObject(IntersectionContinuationChance, 0.75f)
                );
            }
        }

        internal struct ParallelCheckParameters
        {
            /// <summary>
            /// Multiplier of length to check for parallel walls (e.g. 1.5 to check for the length of the proposed wall and half again)
            /// </summary>
            public object Length { get; set; }

            /// <summary>
            /// Distance to check for parallel walls either side
            /// </summary>
            public object Width { get; set; }

            /// <summary>
            /// Maximum angle between this wall and the other to consider parallel
            /// </summary>
            public object Angle { get; set; }
        }
    }

    /// <summary>
    /// Given an outline and a set of seed points (on the outline) grow lines inwards to fill the space
    /// </summary>
    internal class GrowthMap
    {
        #region properties and fields
        private readonly IReadOnlyList<Vector2> _outline;
        private readonly IReadOnlyList<IReadOnlyList<Vector2>> _internalRooms;

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
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outline">Polygon outline of this map (clockwise wound, potentially concave)</param>
        /// <param name="internalRooms">Set of rooms which must be included unchanged in this map</param>
        /// <param name="random">PRNG (0-1)</param>
        /// <param name="metadata">Metadata used in random generation</param>
        /// <param name="parameters"></param>
        public GrowthMap(IReadOnlyList<Vector2> outline, IReadOnlyList<IReadOnlyList<Vector2>> internalRooms, Func<double> random, INamedDataCollection metadata, WallGrowthParameters parameters)
        {
            Contract.Requires(outline != null);
            Contract.Requires(internalRooms != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(parameters != null);

            _outline = outline;
            _internalRooms = internalRooms;
            _random = random;
            _metadata = metadata;

            _seedDistance = parameters.SeedDistance;
            _seedChance = parameters.SeedChance;
            _parallelLengthMultiplier = parameters.ParallelLengthMultiplier;
            _parallelCheckWidth = parameters.ParallelCheckWidth;
            _cosineParallelAngleThreshold = parameters.ParallelAngleThreshold.Transform(a => (float)Math.Cos(a));
            _intersectionContinuationChance = parameters.IntersectionContinuationChance;

            var bounds = BoundingRectangle.CreateFromPoints(outline).Inflate(0.2f);
            _mesh = new Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>(bounds);
        }

        public Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> Grow()
        {
            //Create initial seeds along the outline of the building
            CreateOutline(_outline, false);
            foreach (var room in _internalRooms)
                CreateOutline(room.Reverse(), true);  //todo: <-- pass in additional detail about this vertical feature, create face and tag it with additional info

            //Pull seeds out of heap and grow them (eventually we will run out of valid seeds and exit this loop)
            while (_seeds.Count > 0)
                GrowSeed(_seeds.RemoveMin());

            //Sometimes we create edges which lead to a vertex and then nothing else, remove all these dead end edges
            CleanupDeadEnds();

            //Put faces in between the walls we've created
            _mesh.CreateImplicitFaces(f => new FloorplanFaceTag(true));

            //Remove vertices which lie on a perfectly straight line with no branches
            _mesh.SimplifyFaces();

            return ConvertToMesh();
        }

        #region growth
        private void GrowSeed(Seed seed)
        {
            Contract.Requires(seed != null);

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
                    ab.Tag = new FloorplanHalfEdgeTag(t.IsImpassable);
                    bc.Tag = new FloorplanHalfEdgeTag(t.IsImpassable);
                }

                //Create an edge to this intersection point
                _mesh.GetOrConstructHalfEdge(seed.Origin, intersectVert).Tag = new FloorplanHalfEdgeTag(false);

                //If this is not an external wall we can create a seed continuing forward
                var tag = firstIntersection.Value.Key.Tag ?? firstIntersection.Value.Key.Pair.Tag;
                var continuationChance = _intersectionContinuationChance.SelectFloatValue(_random, _metadata);
                if (!tag.IsImpassable && _random.RandomBoolean(1 - continuationChance))
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
            Contract.Requires(seed != null);

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
            //this method seems rather trivial! Why is it here?
            //If we want to change the *tags* which we emit publically from this class this is where we could take the internal mesh with private tag types and convert...
            //...to the public tag types
            return _mesh;
        }
        #endregion

        #region initialisation
        private void CreateOutline(IEnumerable<Vector2> shape, bool createFace)
        {
            Contract.Requires(shape != null);

            var vertices = (IReadOnlyList<MVertex>)shape.Select(_mesh.GetOrConstructVertex).ToArray();
            var edges = new List<MHEdge>(vertices.Count * 3);

            // Create the outer edges of the floor
            for (var i = 0; i < vertices.Count; i++)
            {
                //Start and end vertex of this wall
                var b = vertices[i];
                var c = vertices[(i + 1) % vertices.Count];

                //Create a series of edges between these two vertices (not just one edge, because we drop seeds along the line as we go)
                CreateImpassableEdge(b, c, edges);

                //We want to measure the internal angle at vertex "b", for that we need the previous vertex (which we'll call "a")
                var a = vertices[(i + vertices.Count - 1) % vertices.Count];

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

            if (createFace)
            {
                //Ensure we're always creating the clockwise face
                if (!edges.Select(a => a.EndVertex.Position).IsClockwise())
                {
                    edges.Reverse();
                    for (var i = 0; i < edges.Count; i++)
                        edges[i] = edges[i].Pair;
                }

                //Create face
                //todo: attach spacespec metadata (passed in instead of bool:createFace)
                var f = _mesh.GetOrConstructFace(edges);
                f.Tag = new FloorplanFaceTag(false);
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
        /// <param name="edges"></param>
        /// <returns></returns>
        private void CreateImpassableEdge(MVertex a, MVertex b, ICollection<MHEdge> edges = null)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);

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
                var edge = _mesh.GetOrConstructHalfEdge(current, v);
                edge.Tag = new FloorplanHalfEdgeTag(true);
                if (edges != null)
                    edges.Add(edge);

                current = v;
            }

            //Finish off the end of the edge
            var finalEdge = _mesh.GetOrConstructHalfEdge(current, b);
            finalEdge.Tag = new FloorplanHalfEdgeTag(true);
            if (edges != null)
                edges.Add(finalEdge);
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
