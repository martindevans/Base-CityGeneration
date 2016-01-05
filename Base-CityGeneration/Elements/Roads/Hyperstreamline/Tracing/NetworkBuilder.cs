using System.Numerics;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using HandyCollections.Geometry;
using HandyCollections.Heap;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Management.Instrumentation;
using Base_CityGeneration.Utilities.Extensions;
using Myre.Collections;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing
{
    public class NetworkBuilder
    {
        private Quadtree<Vertex> _vertices;
        private Quadtree<Edge> _edges;

        private readonly HashSet<Streamline> _streams;

        private bool _begun;
        private Vector2 _min;
        private Vector2 _max;

        public NetworkBuilder()
        {
            _streams = new HashSet<Streamline>();
        }

        #region finalisation
        public Network Result
        {
            get
            {
                Contract.Ensures(Contract.Result<Network>() != null);
                return new Network(
                    Vertices()
                );
            }
        }

        public void Reduce()
        {
            foreach (var streamline in _streams)
                LinearReduction(streamline);
        }
        #endregion

        #region building
        public void Begin(Vector2 min, Vector2 max)
        {
            if (_begun)
                throw new InvalidOperationException("Cannot call Begin twice");

            _min = min;
            _max = max;

            _vertices = new Quadtree<Vertex>(new BoundingRectangle(new Vector2(min.X, min.Y), new Vector2(max.X, max.Y)), 63);
            _edges = new Quadtree<Edge>(new BoundingRectangle(new Vector2(min.X, min.Y), new Vector2(max.X, max.Y)), 31);

            _begun = true;
        }

        public void Build(TracingConfiguration config, Func<double> random, INamedDataCollection metadata, Vector2 min, Vector2 max)
        {
            Contract.Requires(config != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            if (!_begun)
            {
                Begin(min, max);
                AddBoundary(min, max);
            }

            var extent = max - min;
            var eigens = config.TensorField.Presample(min, max, (uint)Math.Max(extent.X, extent.Y));

            var count = (int)((extent.X * extent.Y) / 2500);

            Func<Vector2, bool> isOutOfBounds = v => v.X > max.X || v.X < min.X || v.Y > max.Y || v.Y < min.Y;

            var seeds = RandomSeedsInBounds(random, count, eigens.MajorEigenVectors, eigens.MinorEigenVectors, min, max, isOutOfBounds);

            Build(seeds, min, max, config.SeparationField, true, true, config.SegmentLength, config.MergeDistance, config.CosineSearchConeAngle, isOutOfBounds, null, s =>
            {
                s.Width = (uint)config.RoadWidth.SelectIntValue(random, metadata);

                LinearReduction(s);
            });
        }

        public void Build(TracingConfiguration config, Func<double> random, INamedDataCollection metadata, Region region)
        {
            Contract.Requires(config != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(region != null);

            //var extent = region.Max - region.Min;
            //var eigens = config.TensorField.Presample(new Vector2(region.Min.X, region.Min.Y), new Vector2(region.Max.X, region.Max.Y), (int)Math.Max(extent.X, extent.Y));

            var ex = _max - _min;
            var eigens = config.TensorField.Presample(_min, _max, (uint)Math.Max(ex.X, ex.Y));

            var seeds = SeedsAlongEdge(region, config.SeparationField, eigens.MajorEigenVectors, eigens.MinorEigenVectors);

            Build(seeds, region.Min, region.Max,
                config.SeparationField,
                true, true,
                config.MergeDistance,
                config.SegmentLength,
                config.CosineSearchConeAngle,
                p => !region.PointInPolygon(p), e => e.Streamline.Region == region,
                s => {
                    s.Width = (uint)config.RoadWidth.SelectIntValue(random, metadata);
                    s.Region = region;

                    LinearReduction(s);
                });
        }

        private void Build(IEnumerable<Seed> initialSeeds, Vector2 min, Vector2 max, BaseScalarField separation, bool forward, bool backward, float maxSegmentLength, float mergeDistance, float cosineSearchAngle, Func<Vector2, bool> isOutOfBounds, Func<Edge, bool> edgeFilter, Action<Streamline> streamCreated)
        {
            Contract.Requires(initialSeeds != null);
            Contract.Requires(separation != null);
            Contract.Requires(isOutOfBounds != null);
            Contract.Requires(streamCreated != null);

            var seeds = new MinHeap<KeyValuePair<float, Seed>>(1024, new KeyComparer<float, Seed>());
            foreach (var initialSeed in initialSeeds)
                AddSeed(seeds, initialSeed);

            //Trace out roads for every single seed
            while (seeds.Count > 0)
            {
                var s = RemoveSeed(seeds, separation, cosineSearchAngle, edgeFilter);
                if (!s.HasValue)
                    continue;

                if (forward)
                {
                    var stream = CheckStream(Trace(s.Value, false, seeds, isOutOfBounds, maxSegmentLength, mergeDistance, cosineSearchAngle, separation));
                    if (stream != null)
                    {
                        _streams.Add(stream);
                        streamCreated(stream);
                    }
                }

                if (backward)
                {
                    var stream = CheckStream(Trace(s.Value, true, seeds, isOutOfBounds, maxSegmentLength, mergeDistance, cosineSearchAngle, separation));
                    if (stream != null)
                    {
                        _streams.Add(stream);
                        streamCreated(stream);
                    }
                }
            }
        }

        private void AddBoundary(Vector2 min, Vector2 max)
        {
            //Start boundary
            var boundary = new Streamline(CreateVertex(min));

            //Loop around 3 sides
            InsertEdge(boundary.Extend(CreateVertex(new Vector2(max.X, min.Y))));
            InsertEdge(boundary.Extend(CreateVertex(new Vector2(max.X, max.Y))));
            InsertEdge(boundary.Extend(CreateVertex(new Vector2(min.X, max.Y))));

            //Close loop
            InsertEdge(boundary.Extend(boundary.First));
        }
        #endregion

        #region streams
        private void LinearReduction(Streamline stream)
        {
            Contract.Requires(stream != null);

            //Helper functions
            var visited = new HashSet<Edge>();
            Func<Edge, bool> predicate = e => e.Streamline == stream;
            Func<Vertex, Edge> next = v => v.Edges.Where(predicate).Where(a => !visited.Contains(a)).SingleOrDefault(e => e.A.Equals(v));

            //Setup, we'll advance "end" forwards, reducing edges which are straight, until end is the end of the streamline
            var start = stream.First;
            var se = next(start);
            if (se == null)
                return;
            var end = se.B;
            var segmentDirection = Vector2.Normalize(end.Position - start.Position);

            while (end.EdgeCount != 1)
            {
                visited.Add(se);

                //If we hit a branching vertex skip forwards
                while (end.EdgeCount > 2)
                {
                    start = end;
                    se = next(start);
                    if (se == null)
                        return;
                    end = se.B;
                    segmentDirection = Vector2.Normalize(end.Position - start.Position);
                }


                var n = next(end);
                if (n == null)
                    return;

                var dir = Vector2.Normalize(n.B.Position - start.Position);

                if (Vector2.Dot(segmentDirection, dir) > 0.999)
                {
                    //Reduce

                    //Delete old components
                    DeleteEdge(se);
                    DeleteEdge(n);
                    DeleteVertex(end);

                    //Remove mid vertex from stream
                    stream.Remove(end);

                    //Create new edge
                    se = Edge.Create(stream, start, n.B);
                    InsertEdge(se);

                    //Move end pointer
                    end = n.B;
                }
                else
                {
                    start = end;
                    se = n;
                    end = se.B;
                    segmentDirection = Vector2.Normalize(end.Position - start.Position);
                }
            }
        }

        private Streamline CheckStream(Streamline stream)
        {
            if (stream == null)
                return null;

            if (stream.Count == 1)
            {
                DeleteStream(stream);
                return null;
            }

            return stream;
        }

        private void DeleteStream(Streamline streamline)
        {
            Contract.Requires(streamline != null);

            foreach (var vertex in streamline.Vertices)
            {
                var edge = vertex.Edges.SingleOrDefault(e => e.Streamline == streamline);
                if (edge != null)
                    DeleteEdge(edge);
            }

            foreach (var vertex in streamline.Vertices)
            {
                if (vertex.EdgeCount == 0)
                    DeleteVertex(vertex);
            }

            _streams.Remove(streamline);
        }

        private Streamline Trace(Seed seed, bool reverse, MinHeap<KeyValuePair<float, Seed>> seeds, Func<Vector2, bool> isOutOfBounds, float maxSegmentLength, float mergeDistance, float cosineSearchAngle, BaseScalarField separation)
        {
            var maxSegmentLengthSquared = maxSegmentLength * maxSegmentLength;

            var seedingDistance = float.MaxValue;
            var direction = Vector2.Zero;
            var position = seed.Point;
            var stream = new Streamline(FindOrCreateVertex(position, mergeDistance, cosineSearchAngle));

            //This is a weird way to do a for loop! What gives?
            //This is, in many respects, a better way to do it if you don't want i to be mutated within the loop
            //In this case I'm using it to pacify a persistent CodeContracts false positive (this loop is too complex for it to analyze, I guess?)
            foreach (var i in Enumerable.Range(0, 10000))
            {
                direction = seed.Field.TraceVectorField(position, direction, maxSegmentLength);
                if (i == 0)
                    direction *= reverse ? -1 : 1;

                //degenerate step check
                var segmentLength = direction.Length();
                if (segmentLength < 0.00005f)
                    break;

                //Excessive step check
                if (segmentLength > maxSegmentLength)
                {
                    direction /= segmentLength * maxSegmentLength;
                    segmentLength = maxSegmentLength;
                }

                //Step along path
                position += direction;
                seedingDistance += segmentLength;

                //Bounds check
                if (isOutOfBounds(position))
                {
                    CreateEdge(stream, position, Vector2.Normalize(direction), maxSegmentLength, maxSegmentLengthSquared, mergeDistance, cosineSearchAngle, skipDistanceCheck: true);
                    break;
                }

                //Create the segment and break if it says so
                if (CreateEdge(stream, position, Vector2.Normalize(direction), maxSegmentLength, maxSegmentLengthSquared, mergeDistance, cosineSearchAngle))
                    break;

                //Accumulate seeds to trace into the alternative field
                var seedSeparation = separation.Sample(position);
                if (seedingDistance > seedSeparation)
                {
                    seedingDistance = 0;
                    AddSeed(seeds, new Seed(position, seed.AlternativeField, seed.Field));
                }
            }

            return stream;
        }
        #endregion

        public IEnumerable<Region> Regions()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Region>>() != null);
            return new RegionBuilder(Vertices()).Regions();
        }

        #region edges
        private bool CreateEdge(Streamline streamline, Vector2 endPosition, Vector2 direction, float segmentLength, float segmentLengthSquared, float mergeDistance, float cosineSearchAngle, bool skipDistanceCheck = false)
        {
            Contract.Requires(streamline != null);

            //Do not create an edge if this distance is too short
            if (!skipDistanceCheck && Vector2.DistanceSquared(streamline.Last.PositionField, endPosition) < segmentLengthSquared)
                return false;

            //Check if the position of the line has wandered close to an already existing vertex
            Vertex endVertex = FindVertex(endPosition, direction, mergeDistance, cosineSearchAngle, streamline.Last);

            //Check if this proposed new edge intersects another already existing edge
            Vector2 intersectPosition;
            var intersect = FindIntersectingEdge(streamline.Last.Position, endVertex == null ? endPosition : endVertex.Position, out intersectPosition);
            if (intersect != null)
            {
                //If we're close enough to either end, use that end instead of splitting the edge
                var dist = segmentLength * 0.2f;
                if ((intersectPosition - intersect.A.Position).Length() < dist)
                    endVertex = intersect.A;
                else if ((intersectPosition - intersect.B.Position).Length() < dist)
                    endVertex = intersect.B;
                else
                {
                    //Nope, we've gotta split the edge
                    Edge aMid, midB;
                    endVertex = intersect.Split(FindOrCreateVertex(intersectPosition, dist, 0), out aMid, out midB);
                    DeleteEdge(intersect);
                    InsertEdge(aMid);
                    InsertEdge(midB);
                }

                //Console.WriteLine(endPosition.GetHashCode() + " Found intersecting edge");
            }

            //If we've not found a vertex by any of the above, the line can continue
            bool endOfLine = endVertex != null;

            //Haven't moved very far, trying to connect a vertex to itself
            if (endVertex != null && endVertex.Equals(streamline.Last))
                return false;

            //Didn't find one, create a new vertex
            if (endVertex == null)
                endVertex = CreateVertex(endPosition);

            //Extend the streamline (endOfLine if for some reason an edge isn't created)
            if (!streamline.Contains(endVertex) || streamline.First.Equals(endVertex))
                endOfLine |= !InsertEdge(streamline.Extend(endVertex));

            //Wrapped around
            endOfLine |= streamline.First.Equals(streamline.Last);

            //Return an indication of if we have hit the end of the line by merging into another
            return endOfLine;
        }

        private bool InsertEdge(Edge e)
        {
            Contract.Requires(_edges != null);

            if (e == null)
                return false;

            _edges.Insert(new BoundingRectangle(
                new Vector2(Math.Min(e.A.Position.X, e.B.Position.X), Math.Min(e.A.Position.Y, e.B.Position.Y)),
                new Vector2(Math.Max(e.A.Position.X, e.B.Position.X), Math.Max(e.A.Position.Y, e.B.Position.Y))
            ), e);

            return true;
        }

        /// <summary>
        /// Find edges close to given position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        private IEnumerable<Edge> FindEdges(Vector2 position, float radius)
        {
            Contract.Requires(_vertices != null);
            Contract.Ensures(Contract.Result<IEnumerable<Edge>>() != null);

            return _vertices.Intersects(
                new BoundingRectangle(
                    new Vector2(position.X - radius, position.Y - radius),
                    new Vector2(position.X + radius, position.Y + radius)
                )
            ).SelectMany(v => v.Edges).Distinct();
        }

        private Edge FindIntersectingEdge(Vector2 a, Vector2 b, out Vector2 intersectPosition)
        {
            var min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            var max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

            //Select the all intersections (by checking all edges in octree result)
            var segment = new LineSegment2(a, b);
            var best = (from candidate in _edges.Intersects(new BoundingRectangle(min, max))
                        let intersectionMaybe = new LineSegment2(candidate.A.Position, candidate.B.Position).Intersects(segment)
                        where intersectionMaybe.HasValue
                        let intersection = intersectionMaybe.Value
                        select new KeyValuePair<LinesIntersection2, Edge>(intersection, candidate));

            intersectPosition = Vector2.Zero;
            Edge bestEdge = null;
            var bestScore = float.MaxValue;
            foreach (var keyValuePair in best)
            {
                if (keyValuePair.Key.DistanceAlongB < bestScore)
                {
                    intersectPosition = keyValuePair.Key.Position;
                    bestEdge = keyValuePair.Value;
                    bestScore = keyValuePair.Key.DistanceAlongB;
                }
            }

            return bestEdge;
        }

        private void DeleteEdge(Edge e)
        {
            Contract.Requires(e != null);
            Contract.Requires(_edges != null);

            //Remove from vertices
            e.A.Remove(e);
            e.B.Remove(e);

            //Create bounding rectangle (with 1 padding in either direction)
            var a = e.A.Position;
            var b = e.B.Position;
            var min = new Vector2(Math.Min(a.X, b.X) - 1, Math.Min(a.Y, b.Y) - 1);
            var max = new Vector2(Math.Max(a.X, b.X) + 1, Math.Max(a.Y, b.Y) + 1);

            //Remove from quadtree index
            if (!_edges.Remove(new BoundingRectangle(min, max), e))
                throw new InstanceNotFoundException("Failed to delete edge");
        }
        #endregion

        #region vertices
        private Vertex FindOrCreateVertex(Vector2 position, float mergeDistance, float cosineSearchAngle)
        {
            Contract.Requires(_vertices != null);
            Contract.Ensures(Contract.Result<Vertex>() != null);

            return FindVertex(position, Vector2.Zero, mergeDistance, cosineSearchAngle, null)
                ?? CreateVertex(position);
        }

        private Vertex FindVertex(Vector2 position, Vector2 direction, float radius, float cosineSearchAngle, Vertex skip)
        {
            Contract.Requires(_vertices != null);

            var candidates = _vertices.Intersects(new BoundingRectangle(new Vector2(position.X - radius, position.Y - radius), new Vector2(position.X + radius, position.Y + radius)));

            Vertex closest = null;
            var closestDistanceSqr = float.MaxValue;
            foreach (var candidate in candidates)
            {
                if (candidate.Equals(skip))
                    continue;

                var v = candidate.Position - position;

                //Select the closest candidate
                var dSqr = v.LengthSquared();
                if (dSqr > closestDistanceSqr)
                    continue;

                //Only select vertices which are within a cone in the direction of travel
                if (direction != Vector2.Zero)
                {
                    var dot = Vector2.Dot(v/(float) Math.Sqrt(dSqr), direction);
                    if (dot < cosineSearchAngle)
                        continue;
                }

                closestDistanceSqr = dSqr;
                closest = candidate;
            }

            //The query is a circle, but we checked a rectangle, reject if it's in the rect but outside the circle
            if (Math.Sqrt(closestDistanceSqr) > radius)
                return null;

            return closest;
        }

        private Vertex CreateVertex(Vector2 position)
        {
            var overlaps = _vertices.ContainedBy(new BoundingRectangle(
                new Vector2(position.X - 1, position.Y - 1),
                new Vector2(position.X + 1, position.Y + 1)
            ));
            if (overlaps.Any(a => a.Position == position))
                throw new InvalidOperationException();

            var vertex = new Vertex(position);

            _vertices.Insert(
                new BoundingRectangle(
                    new Vector2(position.X, position.Y),
                    new Vector2(position.X, position.Y)
                ),
                vertex
            );

            return vertex;
        }

        private bool DeleteVertex(Vertex vertex)
        {
            Contract.Requires(vertex != null);
            Contract.Requires(vertex.EdgeCount == 0, "Cannot delete vertex with attached edges");

            var pos = new Vector2(vertex.Position.X, vertex.Position.Y);
            return _vertices.Remove(new BoundingRectangle(pos, pos), vertex);
        }
        #endregion

        #region seeds
        private static IEnumerable<Seed> SeedsAlongEdge(Region region, BaseScalarField distanceField, IVector2Field major, IVector2Field minor)
        {
            float d = 0;
            Vector2? previous = null;
            foreach (var vertex in region.Vertices)
            {
                if (previous.HasValue)
                {
                    var pos = previous.Value;
                    var v = vertex - previous.Value;
                    var length = v.Length();
                    var dir = v / length;

                    for (int i = 0; i < length; i++)
                    {
                        var separation = distanceField.Sample(pos + dir * i);
                        d += separation;
                        if (d >= separation)
                        {
                            yield return new Seed(pos + dir * i, major, minor);
                            d -= separation;
                        }
                    }
                }
                previous = vertex;
            }
        }

        private static IEnumerable<Seed> RandomSeedsInBounds(Func<double> random, int count, IVector2Field major, IVector2Field minor, Vector2 min, Vector2 max, Func<Vector2, bool> isOutOfBounds)
        {
            var extent = (max - min);

            for (int i = 0; i < count; i++)
            {
                var p = new Vector2((float)random() * extent.X, (float)random() * extent.Y) + min;
                if (isOutOfBounds(p))
                    i--;
                else
                    yield return new Seed(p, major, minor);
            }
        }

        private Seed? RemoveSeed(IMinHeap<KeyValuePair<float, Seed>> seeds, BaseScalarField separation, float cosineSearchAngle, Func<Edge, bool> edgeFilter = null)
        {
            Contract.Requires(seeds != null);
            Contract.Requires(separation != null);

            while (seeds.Count > 0)
            {
                //Get the highest priority seed
                var s = seeds.RemoveMin().Value;

                //Check if it's valid
                var d = s.Field.Sample(s.Point);

                //Degenerate point?
                var l = d.Length();
                if (l < 0.001f)
                    continue;

                var sep = separation.Sample(s.Point);

                //Normalize direction
                d /= l;

                //Get edges near this point and check if there is a parallel edge
                if (FindEdges(s.Point, sep).Where(e => edgeFilter == null || edgeFilter(e)).Any(e => Math.Abs(Vector2.Dot(e.Direction, d)) > cosineSearchAngle))
                    continue;

                return s;
            }

            //No valid seeds found
            return null;
        }

        private static void AddSeed(IMinHeap<KeyValuePair<float, Seed>> seeds, Seed seed, BaseScalarField priority = null, BaseScalarField separation = null)
        {
            Contract.Requires(seeds != null);

            var prio = priority.SafeSample(seed.Point)
                     + 1 / separation.SafeSample(seed.Point);

            seeds.Add(new KeyValuePair<float, Seed>(-prio, seed));
        }

        private IEnumerable<Vertex> Vertices()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Vertex>>() != null);

            return _vertices.Intersects(
                new BoundingRectangle(
                    new Vector2(float.MinValue),
                    new Vector2(float.MaxValue)
                )
            ).Where(a => a.EdgeCount > 0);
        }
        #endregion
    }
}
