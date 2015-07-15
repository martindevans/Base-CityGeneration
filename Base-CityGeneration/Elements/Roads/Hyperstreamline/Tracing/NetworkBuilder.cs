﻿using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Scalars;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Tensors;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Fields.Vectors;
using EpimetheusPlugins.Procedural.Utilities;
using HandyCollections.Geometry;
using HandyCollections.Heap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using HGVector2 = HandyCollections.Geometry.Vector2;
using Vector2 = Microsoft.Xna.Framework.Vector2;

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

            _vertices = new Quadtree<Vertex>(new BoundingRectangle(new HGVector2(min.X, min.Y), new HGVector2(max.X, max.Y)), 63);
            _edges = new Quadtree<Edge>(new BoundingRectangle(new HGVector2(min.X, min.Y), new HGVector2(max.X, max.Y)), 31);

            _begun = true;
        }

        public void Build(TracingConfiguration config, Random random, Vector2 min, Vector2 max)
        {
            if (!_begun)
            {
                Begin(min, max);
                AddBoundary(min, max);
            }

            var extent = max - min;
            var eigens = config.TensorField.Presample(min, max, (int)Math.Max(extent.X, extent.Y));

            var count = (int)((extent.X * extent.Y) / 2500);

            Func<Vector2, bool> isOutOfBounds = v => v.X > max.X || v.X < min.X || v.Y > max.Y || v.Y < min.Y;

            var seeds = RandomSeedsInBounds(random, count, eigens.MajorEigenVectors, eigens.MinorEigenVectors, min, max, isOutOfBounds);

            Build(seeds, min, max, config.SeparationField, true, true, config.SegmentLength, config.MergeDistance, config.CosineSearchConeAngle, isOutOfBounds, null, s =>
            {
                s.Width = config.RoadWidth.SelectIntValue(random.NextDouble);

                LinearReduction(s);
            });
        }

        public void Build(TracingConfiguration config, Random random, Region region)
        {
            var extent = region.Max - region.Min;
            //var eigens = config.TensorField.Presample(new Vector2(region.Min.X, region.Min.Y), new Vector2(region.Max.X, region.Max.Y), (int)Math.Max(extent.X, extent.Y));

            var ex = _max - _min;
            var eigens = config.TensorField.Presample(_min, _max, (int)Math.Max(ex.X, ex.Y));

            var seeds = SeedsAlongEdge(region, config.SeparationField, eigens.MajorEigenVectors, eigens.MinorEigenVectors);

            Build(seeds, region.Min, region.Max,
                config.SeparationField,
                true, true,
                config.MergeDistance,
                config.SegmentLength,
                config.CosineSearchConeAngle,
                p => !region.PointInPolygon(p), e => e.Streamline.Region == region,
                s => {
                    s.Width = config.RoadWidth.SelectIntValue(random.NextDouble);
                    s.Region = region;

                    LinearReduction(s);
                });
        }

        private void Build(IEnumerable<Seed> initialSeeds, Vector2 min, Vector2 max, BaseScalarField separation, bool forward, bool backward, float maxSegmentLength, float mergeDistance, float cosineSearchAngle, Func<Vector2, bool> isOutOfBounds, Func<Edge, bool> edgeFilter, Action<Streamline> streamCreated)
        {
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
            Streamline boundary = new Streamline(CreateVertex(min));

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
            //Helper functions
            HashSet<Edge> visited = new HashSet<Edge>();
            Func<Edge, bool> predicate = e => e.Streamline == stream;
            Func<Vertex, Edge> next = v => v.Edges.Where(predicate).Where(a => !visited.Contains(a)).SingleOrDefault(e => e.A.Equals(v));

            //Setup, we'll advance "end" forwards, reducing edges which are straight, until end is the end of the streamline
            Vertex start = stream.First;
            Edge se = next(start);
            if (se == null)
                return;
            Vertex end = se.B;
            var segmentDirection = Vector2.Normalize(end.Position - start.Position);

            while (end != null && end.EdgeCount != 1)
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
                    se = next(start);
                    if (se == null)
                        return;
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

        private void DeleteStream(Streamline stream)
        {
            foreach (var vertex in stream.Vertices)
            {
                var edge = vertex.Edges.SingleOrDefault(e => e.Streamline == stream);
                if (edge != null)
                    DeleteEdge(edge);
            }

            foreach (var vertex in stream.Vertices)
            {
                if (vertex.EdgeCount == 0)
                    DeleteVertex(vertex);
            }

            _streams.Remove(stream);
        }

        private Streamline Trace(Seed seed, bool reverse, MinHeap<KeyValuePair<float, Seed>> seeds, Func<Vector2, bool> isOutOfBounds, float maxSegmentLength, float mergeDistance, float cosineSearchAngle, BaseScalarField separation)
        {
            var maxSegmentLengthSquared = maxSegmentLength * maxSegmentLength;

            var seedingDistance = float.MaxValue;
            var direction = Vector2.Zero;
            var position = seed.Point;
            var stream = new Streamline(FindOrCreateVertex(position, mergeDistance, cosineSearchAngle));
            for (var i = 0; i < 10000; i++)
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
            var r = new RegionBuilder(Vertices());
            return r.Regions();
        }

        #region edges
        private bool CreateEdge(Streamline streamline, Vector2 endPosition, Vector2 direction, float segmentLength, float segmentLengthSquared, float mergeDistance, float cosineSearchAngle, bool skipDistanceCheck = false)
        {
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
                    endVertex = intersect.Split(CreateVertex(intersectPosition), out aMid, out midB);
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
            if (e == null)
                return false;

            _edges.Insert(new BoundingRectangle(
                new HGVector2(Math.Min(e.A.Position.X, e.B.Position.X), Math.Min(e.A.Position.Y, e.B.Position.Y)),
                new HGVector2(Math.Max(e.A.Position.X, e.B.Position.X), Math.Max(e.A.Position.Y, e.B.Position.Y))
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
            return _vertices.Intersects(
                new BoundingRectangle(
                    new HandyCollections.Geometry.Vector2(position.X - radius, position.Y - radius),
                    new HandyCollections.Geometry.Vector2(position.X + radius, position.Y + radius)
                )
            ).SelectMany(v => v.Edges).Distinct();
        }

        private Edge FindIntersectingEdge(Vector2 a, Vector2 b, out Vector2 intersectPosition)
        {
            var min = new HGVector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            var max = new HGVector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

            var candidates = _edges.Intersects(new BoundingRectangle(min, max));

            intersectPosition = new Vector2(0);
            float d = float.MaxValue;
            Edge selected = null;
            foreach (var candidate in candidates)
            {
                //Ignore edges which start or end in the same place as the query
                if (candidate.A.PositionField.Equals(a) || candidate.B.PositionField.Equals(b))
                    continue;

                //Do we intersect at all?
                var intersection = Geometry2D.LineLineSegmentIntersection(new LineSegment2D(candidate.A.Position, candidate.B.Position), new Line2D(a, b - a));
                if (!intersection.HasValue)
                    continue;

                //Is this intersection closer than the best so far?
                if (intersection.Value.DistanceAlongLineB >= d)
                    continue;

                //Is the intersection *actually* valid (i.e. within the bound of line 2)
                if (intersection.Value.DistanceAlongLineB > 1 || intersection.Value.DistanceAlongLineB < 0)
                    continue;

                //We have a winner, update best fit so far
                d = intersection.Value.DistanceAlongLineB;
                intersectPosition = intersection.Value.Position;
                selected = candidate;
            }

            return selected;
        }

        private void DeleteEdge(Edge e)
        {
            //Remove from vertices
            e.A.Remove(e);
            e.B.Remove(e);

            //Create bounding rectangle (with 1 padding in either direction)
            var a = e.A.Position;
            var b = e.B.Position;
            var min = new HGVector2(Math.Min(a.X, b.X) - 1, Math.Min(a.Y, b.Y) - 1);
            var max = new HGVector2(Math.Max(a.X, b.X) + 1, Math.Max(a.Y, b.Y) + 1);

            //Remove from quadtree index
            if (!_edges.Remove(new BoundingRectangle(min, max), e))
                throw new InstanceNotFoundException("Failed to delete edge");
        }
        #endregion

        #region vertices
        private Vertex FindOrCreateVertex(Vector2 position, float mergeDistance, float cosineSearchAngle)
        {
            return FindVertex(position, Vector2.Zero, mergeDistance, cosineSearchAngle, null)
                ?? CreateVertex(position);
        }

        private Vertex FindVertex(Vector2 position, Vector2 direction, float radius, float cosineSearchAngle, Vertex skip)
        {
            var candidates = _vertices.Intersects(new BoundingRectangle(new HandyCollections.Geometry.Vector2(position.X - radius, position.Y - radius), new HandyCollections.Geometry.Vector2(position.X + radius, position.Y + radius)));

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
            var vertex = new Vertex(position);

            _vertices.Insert(
                new BoundingRectangle(
                    new HandyCollections.Geometry.Vector2(position.X, position.Y),
                    new HandyCollections.Geometry.Vector2(position.X, position.Y)
                ),
                vertex
            );

            return vertex;
        }

        private bool DeleteVertex(Vertex v)
        {
            if (v.EdgeCount != 0)
                throw new InvalidOperationException("Cannot delete vertex with attached edges");

            var pos = new HGVector2(v.Position.X, v.Position.Y);
            return _vertices.Remove(new BoundingRectangle(pos, pos), v);
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

        private static IEnumerable<Seed> RandomSeedsInBounds(Random random, int count, IVector2Field major, IVector2Field minor, Vector2 min, Vector2 max, Func<Vector2, bool> isOutOfBounds)
        {
            var extent = (max - min);

            for (int i = 0; i < count; i++)
            {
                var p = new Vector2((float)random.NextDouble() * extent.X, (float)random.NextDouble() * extent.Y) + min;
                if (isOutOfBounds(p))
                    i--;
                else
                    yield return new Seed(p, major, minor);
            }
        }

        private Seed? RemoveSeed(MinHeap<KeyValuePair<float, Seed>> seeds, BaseScalarField separation, float cosineSearchAngle, Func<Edge, bool> edgeFilter = null)
        {
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

        private void AddSeed(MinHeap<KeyValuePair<float, Seed>> seeds, Seed seed, BaseScalarField priority = null, BaseScalarField separation = null)
        {
            var prio = priority.SafeSample(seed.Point)
                     + 1 / separation.SafeSample(seed.Point);

            seeds.Add(new KeyValuePair<float, Seed>(-prio, seed));
        }

        private IEnumerable<Vertex> Vertices()
        {
            return _vertices.Intersects(
                new BoundingRectangle(
                    new HandyCollections.Geometry.Vector2(float.MinValue),
                    new HandyCollections.Geometry.Vector2(float.MaxValue)
                )
            ).Where(a => a.EdgeCount > 0);
        }
        #endregion
    }
}
