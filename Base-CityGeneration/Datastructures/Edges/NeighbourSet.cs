using System;
using System.Collections.Generic;
using System.Numerics;
using EpimetheusPlugins.Procedural.Utilities;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

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
        private readonly Dictionary<int, List<KeyValuePair<LineSegment2D, T>>> _segments = new Dictionary<int, List<KeyValuePair<LineSegment2D, T>>>();

        public int Count { get; private set; }

        /// <summary>
        /// Find all segments which are neighbours of this line
        /// </summary>
        /// <param name="query"></param>
        /// <param name="angularTolerance">Maximum angular difference (radians) between lines which is still considered parallel</param>
        /// <param name="distanceTolerance">Maximum Distance between lines which is still considered a neighbour</param>
        /// <returns></returns>
        public IEnumerable<NeighbourResult> Neighbours(LineSegment2D query, float angularTolerance, float distanceTolerance)
        {
            var angle = Angle(query);
            var lowKey = ToKey(angle - angularTolerance);
            var highKey = ToKey(angle + angularTolerance + 1);
            var queryLine = query.LongLine();
            var distanceToleranceSq = distanceTolerance * distanceTolerance;

            for (var i = lowKey; i <= highKey; i++)
            {
                List<KeyValuePair<LineSegment2D, T>> segments;

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
                    if (Math.Abs(angle - Angle(segment)) > angularTolerance)
                        continue;

                    //Project points from segment onto query (early exit if the point is too far from the infinite length line)
                    var st = Geometry2D.ClosestPointDistanceAlongLine(queryLine, segment.Start);
                    var sp = queryLine.Point + queryLine.Direction * st;
                    var sd = Vector2.DistanceSquared(segment.Start, sp);
                    if (sd > distanceToleranceSq)
                        continue;

                    var et = Geometry2D.ClosestPointDistanceAlongLine(queryLine, segment.End);
                    var ep = queryLine.Point + queryLine.Direction * et;
                    var ed = Vector2.DistanceSquared(segment.End, ep);
                    if (ed > distanceToleranceSq)
                        continue;

                    //Check if *both* points are beyond the line in the same direction, meaning there is no segment overlap
                    if (st > 1 && et > 1 || st < 0 && et < 0)
                        continue;

                    //Clamp points into the line segment
                    Clamp(ref st, ref sp, queryLine);
                    Clamp(ref et, ref ep, queryLine);

                    var segmentLine = segment.LongLine();
                    yield return new NeighbourResult(
                        segment,
                        Geometry2D.ClosestPointDistanceAlongLine(segmentLine, sp), Geometry2D.ClosestPointDistanceAlongLine(segmentLine, ep),
                        item.Value,
                        st, et
                    );
                }
            }
        }

        private static void Clamp(ref float t, ref Vector2 pointAtT, Line2D line)
        {
            //Clamp into range, or early exit if already within range
            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;
            else
                return;

            //Calculate new point
            pointAtT = line.Point + line.Direction * t;
        }

        public void Add(LineSegment2D segment, T value)
        {
            GetList(Angle(segment)).Add(new KeyValuePair<LineSegment2D, T>(segment, value));
            Count++;
        }

        private static float Angle(LineSegment2D segment)
        {
            var delta = segment.End - segment.Start;
            var angle = (float)Math.Atan2(delta.Y, delta.X);

            while (angle < 0)
                angle += MathHelper.TwoPi;
            return angle % MathHelper.Pi;
        }

        private static int ToKey(float angle)
        {
            var degrees = MathHelper.ToDegrees(angle);
            while (degrees < 0)
                degrees += 360;
            return (int)(degrees % 180);
        }

        private List<KeyValuePair<LineSegment2D, T>> GetList(float angle)
        {
            var key = ToKey(angle);

            List<KeyValuePair<LineSegment2D, T>> list;
            if (!_segments.TryGetValue(key, out list))
            {
                list = new List<KeyValuePair<LineSegment2D, T>>();
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
            public readonly LineSegment2D Segment;

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

            public NeighbourResult(LineSegment2D segment, float segmentOverlapStart, float segmentOverlapEnd, T value, float queryOverlapStart, float queryOverlapEnd)
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
