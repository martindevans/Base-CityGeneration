using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using SwizzleMyVectors.Geometry;
using MathHelperRedux;

namespace Base_CityGeneration.Datastructures.Edges
{
    /// <summary>
    /// A set of line segments, which you can extract potential neighbours from (i.e. overlapping co-linear line segments)
    /// </summary>
    public class NeighbourSet<T>
    {
        /// <summary>
        /// Segments, keyed with their angle rounded down to the nearest integer
        /// </summary>
        private readonly Dictionary<int, List<KeyValuePair<LineSegment2, T>>> _segments = new Dictionary<int, List<KeyValuePair<LineSegment2, T>>>();

        public int Count { get; private set; }

        /// <summary>
        /// Find all segments which are neighbours of this line
        /// </summary>
        /// <param name="query"></param>
        /// <param name="angularTolerance">Maximum angular difference (radians) between lines which is still considered parallel</param>
        /// <param name="distanceTolerance">Maximum Distance between lines which is still considered a neighbour</param>
        /// <param name="antiParallel">Whether the angle of the query line should be inverted before querying</param>
        /// <returns></returns>
        public IEnumerable<NeighbourResult> Neighbours(LineSegment2 query, float angularTolerance, float distanceTolerance, bool antiParallel = false)
        {
            Contract.Ensures(Contract.Result<IEnumerable<NeighbourResult>>() != null);

            var angle = Angle(query, antiParallel);
            var lowKey = ToKey(angle - angularTolerance);
            var highKey = ToKey(angle + angularTolerance);
            var queryLine = query.LongLine;
            var distanceToleranceSq = distanceTolerance * distanceTolerance;

            List<NeighbourResult> results = new List<NeighbourResult>();

            for (var i = lowKey; i <= highKey; i++)
            {
                List<KeyValuePair<LineSegment2, T>> segments;

                // Key cannot be null, it's an int!
                // ReSharper disable once ExceptionNotDocumentedOptional
                if (!_segments.TryGetValue(i, out segments))
                    continue;

                //Check these lines
                foreach (var item in segments)
                {
                    var segment = item.Key;

                    //Do not compare to self
                    if (segment.Equals(query))
                        continue;

                    //Check angular tolerance
                    if (Math.Abs(angle - Angle(segment, false)) > angularTolerance)
                        continue;

                    //Project points from segment onto query (early exit if the point is too far from the infinite length line)
                    var st = queryLine.ClosestPointDistanceAlongLine(segment.Start);
                    var sp = queryLine.Position + queryLine.Direction * st;
                    var sd = Vector2.DistanceSquared(segment.Start, sp);
                    if (sd > distanceToleranceSq)
                        continue;

                    var et = queryLine.ClosestPointDistanceAlongLine(segment.End);
                    var ep = queryLine.Position + queryLine.Direction * et;
                    var ed = Vector2.DistanceSquared(segment.End, ep);
                    if (ed > distanceToleranceSq)
                        continue;

                    //Check if *both* points are beyond the line in the same direction, meaning there is no segment overlap
                    if (st >= 1 && et >= 1 || st <= 0 && et <= 0)
                        continue;

                    //Clamp points into the line segment
                    Clamp(ref st, ref sp, queryLine);
                    Clamp(ref et, ref ep, queryLine);

                    var segmentLine = segment.LongLine;
                    //yield return
                    results.Add(
                        new NeighbourResult(
                            segment,
                            segmentLine.ClosestPointDistanceAlongLine(sp), segmentLine.ClosestPointDistanceAlongLine(ep),
                            item.Value,
                            st, et
                            )
                        );
                }
            }

            return results;
        }

        private static void Clamp(ref float t, ref Vector2 pointAtT, Ray2 line)
        {
            //Clamp into range, or early exit if already within range
            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;
            else
                return;

            //Calculate new point
            pointAtT = line.Position + line.Direction * t;
        }

        public void Add(LineSegment2 segment, T value)
        {
            GetList(Angle(segment, false)).Add(new KeyValuePair<LineSegment2, T>(segment, value));
            Count++;
        }

        private static float Angle(LineSegment2 segment, bool antiParallel)
        {
            var delta = segment.End - segment.Start;
            var angle = (float)Math.Atan2(delta.Y, delta.X);

            if (antiParallel)
                angle += MathHelper.Pi;

            while (angle < 0)
                angle += MathHelper.TwoPi;
            return angle % MathHelper.Pi;
        }

        private static int ToKey(float angle)
        {
            var degrees = angle.ToDegrees();
            while (degrees < 0)
                degrees += 360;
            return (int)(degrees % 180);
        }

        private List<KeyValuePair<LineSegment2, T>> GetList(float angle)
        {
            Contract.Ensures(Contract.Result<List<KeyValuePair<LineSegment2, T>>>() != null);

            var key = ToKey(angle);

            List<KeyValuePair<LineSegment2, T>> list;
            if (!_segments.TryGetValue(key, out list))
            {
                list = new List<KeyValuePair<LineSegment2, T>>();
                _segments.Add(key, list);
            }

            return list;
        }

        /// <summary>
        /// Information about a neighbour relationship
        /// </summary>
        public struct NeighbourResult
        {
            /// <summary>
            /// The segment which is a neighbour of your query
            /// </summary>
            public readonly LineSegment2 Segment;

            /// <summary>
            /// Start point (distance along line of Segment property) of overlap
            /// </summary>
            public readonly float SegmentOverlapStart;

            /// <summary>
            /// End point (distance along line of Segment property) of overlap
            /// </summary>
            public readonly float SegmentOverlapEnd;

            /// <summary>
            /// Start point (distance along line of query) of overlap
            /// </summary>
            public readonly float QueryOverlapStart;

            /// <summary>
            /// End point (distance along line of query) of overlap
            /// </summary>
            public readonly float QueryOverlapEnd;

            /// <summary>
            /// Value associated with this line segment
            /// </summary>
            public readonly T Value; 

            public NeighbourResult(LineSegment2 segment, float segmentOverlapStart, float segmentOverlapEnd, T value, float queryOverlapStart, float queryOverlapEnd)
            {
                Segment = segment;
                SegmentOverlapStart = segmentOverlapStart;
                SegmentOverlapEnd = segmentOverlapEnd;
                Value = value;
                QueryOverlapStart = queryOverlapStart;
                QueryOverlapEnd = queryOverlapEnd;
            }
        }
    }
}
