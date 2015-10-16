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
    public class NeighbourSet
    {
        /// <summary>
        /// Segments, keyed with their angle rounded down to the nearest integer
        /// </summary>
        private readonly Dictionary<int, List<LineSegment2D>> _segments = new Dictionary<int, List<LineSegment2D>>();

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
                List<LineSegment2D> segments;

                // Key cannot be null, it's an int!
                // ReSharper disable once ExceptionNotDocumentedOptional
                if (!_segments.TryGetValue(i, out segments))
                    continue;

                //Check these lines
                foreach (var segment in segments)
                {
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
                    sp = Clamp(st, sp, queryLine);
                    ep = Clamp(et, ep, queryLine);

                    var segmentLine = segment.LongLine();
                    yield return new NeighbourResult(segment, Geometry2D.ClosestPointDistanceAlongLine(segmentLine, sp), Geometry2D.ClosestPointDistanceAlongLine(segmentLine, ep));
                }
            }
        }

        private static Vector2 Clamp(float t, Vector2 pointAtT, Line2D line)
        {
            if (t < 0)
                t = 0;
            else if (t > 1)
                t = 1;
            else
                return pointAtT;

            return line.Point + line.Direction * t;
        }

        public void Add(LineSegment2D segment)
        {
            GetList(Angle(segment)).Add(segment);
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

        private List<LineSegment2D> GetList(float angle)
        {
            var key = ToKey(angle);

            List<LineSegment2D> list;
            if (!_segments.TryGetValue(key, out list))
            {
                list = new List<LineSegment2D>();
                _segments.Add(key, list);
            }

            return list;
        }

        public struct NeighbourResult
        {
            public readonly LineSegment2D Segment;
            public readonly float OverlapStart;
            public readonly float OverlapEnd;

            public NeighbourResult(LineSegment2D segment, float overlapStart, float overlapEnd)
            {
                Segment = segment;
                OverlapStart = overlapStart;
                OverlapEnd = overlapEnd;
            }
        }
    }
}
