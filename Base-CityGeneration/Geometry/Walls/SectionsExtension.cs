using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using ClipperLib;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Geometry.Walls
{
    public static class SectionsExtension
    {
        [Pure]
        public static IEnumerable<Section> Sections(this IEnumerable<Vector2> outer, float width, out Vector2[] innerArr)
        {
            Contract.Requires(outer != null);
            Contract.Requires(width > 0);
            Contract.Ensures(Contract.Result<IEnumerable<Section>>() != null);
            Contract.Ensures(Contract.ValueAtReturn(out innerArr) != null);

            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            return outer.ToArray().Sections(width, out innerArr, out corners);
        }

        /// <summary>
        /// Given 2 polygons (of the same length) which define the inner and outer boundaries of the walls of a room, calculate the sections which make up the wall.
        /// </summary>
        /// <example>
        /// +---+--------+---+
        /// | c | facade | c |
        /// +---+--------+---+
        /// | f |        | f |
        /// | f |        | f |
        /// +---+--------+---+
        /// | c | facade | c |
        /// +---+--------+---+
        /// </example>
        /// <param name="outer">The outer boundary of the wall</param>
        /// <param name="width">The thickness of the walls</param>
        /// <param name="innerArr"></param>
        /// <param name="corners"></param>
        /// <returns>Sections of the wall. Facades are the flat sections, and corners join facades.</returns>
        [Pure]
        public static IEnumerable<Section> Sections(this IReadOnlyList<Vector2> outer, float width, out Vector2[] innerArr, out IReadOnlyList<IReadOnlyList<Vector2>> corners)
        {
            Contract.Requires(outer != null);
            Contract.Requires(width > 0);
            Contract.Ensures(Contract.Result<IEnumerable<Section>>() != null);

            innerArr = outer.Shrink(width, 1).ToArray();

            //Sanity check if shrinking has deleted the room!
            if (innerArr.Length == 0)
            {
                corners = new IReadOnlyList<Vector2>[0];
                return Array.Empty<Section>();
            }

            //Create a place to put the results
            var results = new List<Section>(innerArr.Length * 2);

            //loop over segments of inner array
            //inner array length is always <= outer array length
            for (var i = 0; i < innerArr.Length; i++)
            {
                var a = innerArr[i];
                var b = innerArr[(i + 1) % innerArr.Length];
                var innerSegment = new LineSegment2(a, b);

                //find a parallel segment which we can project the inner segment onto completely
                float aT;
                float bT;
                var outerSegmentIndex = FindOuterSegment(innerSegment, width, outer, out aT, out bT);

                if (outerSegmentIndex == -1)
                    continue;

                var outerSegment = new LineSegment2(outer[outerSegmentIndex], outer[(outerSegmentIndex + 1) % outer.Count]);

                //Clamp into the valid range
                var adjustedAT = aT.Clamp(0, 1);
                var adjustedBT = bT.Clamp(0, 1);

                //Calculate the locations of the adjusted points
                var aP = outerSegment.LongLine.PointAlongLine(adjustedAT);
                var bP = outerSegment.LongLine.PointAlongLine(adjustedBT);

                //Now adjust the inner points to match the adjusted outer points
                var innerStart = innerSegment.Start;
                if (Math.Abs(adjustedAT - aT) > float.Epsilon)
                    innerStart = innerSegment.ClosestPoint(aP);

                var innerEnd = innerSegment.End;
                if (Math.Abs(adjustedBT - bT) > float.Epsilon)
                    innerEnd = innerSegment.ClosestPoint(bP);

                results.Add(new Section(innerEnd, innerStart, aP, bP));
            }

            //Calculate corner sections
            corners = CalculateCorners(outer, width, results);

            return results;
        }

        private static IReadOnlyList<IReadOnlyList<Vector2>> CalculateCorners(IReadOnlyList<Vector2> outer, float width, IReadOnlyList<Section> sections)
        {
            const float TOLERANCE = 0.01f;

            //Reform inner boundary, but slightly larger than it should be (that's our error tolerance);
            var inner = outer.Shrink(width - TOLERANCE, 1);

            //Reform outer boundary, but slightly smaller than it should be (more error tolerance)
            var outerAdjusted = outer.Shrink(TOLERANCE, 1000);

            var clipper = new Clipper();
            //Create a ring shape which is all the outer walls of this room (outer - inner)
            clipper.AddPath(outerAdjusted.Select(ToPoint).ToList(), PolyType.ptSubject, true);
            clipper.AddPath(inner.Select(ToPoint).ToList(), PolyType.ptClip, true);
            var ringResult = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctDifference, ringResult);

            //Now subtract all the wall sections
            clipper.Clear();
            clipper.AddPaths(ringResult, PolyType.ptSubject, true);
            foreach (var section in sections)
                clipper.AddPath(new List<IntPoint> { ToPoint(section.Inner1), ToPoint(section.Inner2), ToPoint(section.Outer1), ToPoint(section.Outer2) }, PolyType.ptClip, true);
            var result = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctDifference, result);

            //Convert back to vectors
            return result.Select(a => a.Select(ToVector).ToArray()).ToArray();
        }

        private static IntPoint ToPoint(Vector2 v)
        {
            return new IntPoint((int)(v.X * 1000), (int)(v.Y * 1000));
        }

        private static Vector2 ToVector(IntPoint i)
        {
            return new Vector2(i.X / 1000f, i.Y / 1000f);
        }

        /// <summary>
        /// Find a segment which is parallel and the correct distance away from another segment
        /// </summary>
        /// <param name="inner">Inner segment we're trying to find an outer segment for</param>
        /// <param name="distance">perpendicular distance which should be between the two segments</param>
        /// <param name="outer">List of outer vertices to form outer segments</param>
        /// <param name="aT">distance along returned segment to the projected start of the inner segment</param>
        /// <param name="bT">distance along returned segment to the projected end of the inner segment</param>
        /// <returns>The index of the segment which is parallel and the correct distance away</returns>
        private static int FindOuterSegment(LineSegment2 inner, float distance, IReadOnlyList<Vector2> outer, out float aT, out float bT)
        {
            Contract.Requires(outer != null);
            Contract.Requires(distance > 0);

            const float TOLERANCE = 0.01f;

            var innerDir = inner.Line.Direction;

            for (var i = 0; i < outer.Count; i++)
            {
                var seg = new LineSegment2(outer[i], outer[(i + 1) % outer.Count]);
                var dot = Vector2.Dot(seg.Line.Direction, innerDir);
                if (dot >= 0.99f)
                {
                    //This segment is parallel, but is it the correct distance away
                    aT = seg.LongLine.ClosestPointDistanceAlongLine(inner.Start);
                    var aD = Vector2.Distance(inner.Start, seg.LongLine.PointAlongLine(aT));
                    if (!aD.TolerantEquals(distance, TOLERANCE))
                        continue;

                    bT = seg.LongLine.ClosestPointDistanceAlongLine(inner.End);
                    var bD = Vector2.Distance(inner.End, seg.LongLine.PointAlongLine(bT));
                    if (!bD.TolerantEquals(distance, TOLERANCE))
                        continue;

                    //If the overlap is totally off one end of the sections, find something better
                    if ((aT > 1 && bT > 1) || (aT < 0 && bT < 0))
                        continue;

                    return i;
                }
            }

            aT = 0;
            bT = 0;
            return -1;
        }

        [Pure]
        public static IEnumerable<Section> Sections(this IReadOnlyList<Vector2> outer, float width)
        {
            Contract.Requires(outer != null);
            Contract.Requires(width > 0);
            Contract.Ensures(Contract.Result<IEnumerable<Section>>() != null);

            Vector2[] _;
            return Sections(outer, width, out _);
        }
    }

    /// <summary>
    /// A section of a wall.
    /// </summary>
    public struct Section
    {
        #region fields and properties
        private readonly Vector2 _normal;
        /// <summary>
        /// Points from the inside to the outside
        /// </summary>
        public Vector2 Normal
        {
            get
            {
                return _normal;
            }
        }

        private readonly float _thickness;
        /// <summary>
        /// The thickness of the wall along the normal
        /// </summary>
        public float Thickness
        {
            get
            {
                return _thickness;
            }
        }

        private readonly Vector2 _along;
        /// <summary>
        /// Points from one side to the other (across, but not necessarily perpendicular to, the normal)
        /// </summary>
        public Vector2 Along
        {
            get
            {
                return _along;
            }
        }

        private readonly float _width;
        /// <summary>
        /// The size of the wall along the "along" vector
        /// </summary>
        public float Width
        {
            get
            {
                return _width;
            }
        }

        private readonly Vector2 _inner1;

        /// <summary>
        /// Inside 1
        /// </summary>
        public Vector2 Inner1
        {
            get
            {
                return _inner1;
            }
        }

        private readonly Vector2 _inner2;

        /// <summary>
        /// Inside 2
        /// </summary>
        public Vector2 Inner2
        {
            get
            {
                return _inner2;
            }
        }

        private readonly Vector2 _outer1;

        /// <summary>
        /// Outside 1
        /// </summary>
        public Vector2 Outer1
        {
            get
            {
                return _outer1;
            }
        }

        private readonly Vector2 _outer2;

        /// <summary>
        /// Outside 2
        /// </summary>
        public Vector2 Outer2
        {
            get
            {
                return _outer2;
            }
        }

        public LineSegment2 ExternalLineSegment { get { return new LineSegment2(_outer1, _outer2); } }
        public LineSegment2 InternalLineSegment { get { return new LineSegment2(_inner1, _inner2); } }
        #endregion

        #region constructor
        public Section(Vector2 inside1, Vector2 inside2, Vector2 outside2, Vector2 outside1)
        {
            _inner1 = inside1;
            _inner2 = inside2;
            _outer1 = outside2;
            _outer2 = outside1;

            var normal = outside1 - inside1;
            _thickness = normal.Length();
            _normal = normal / _thickness;

            var width = outside2 - outside1;
            _width = width.Length();
            _along = width / _width;
        }
        #endregion

        public Vector2[] GetCorners(Vector2[] corners = null)
        {
            Contract.Requires(corners == null || corners.Length >= 4);
            Contract.Ensures(Contract.Result<IList<Vector2>>() != null);

            if (corners == null)
                corners = new Vector2[4];

            corners[0] = Inner1;
            corners[1] = Inner2;
            corners[2] = Outer1;
            corners[3] = Outer2;

            return corners;
        }

        /// <summary>
        /// Determines if 2 sections are the same (except for rotational differences). e.g. ABCD == BCDA
        /// </summary>
        /// <param name="other"></param>
        /// <param name="epsilon"></param>
        /// <returns></returns>
        public bool Matches(Section other, float epsilon = 0.01f)
        {
            if (Inner1.TolerantEquals(other.Inner1, epsilon))
            {
                return Inner2.TolerantEquals(other.Inner2, epsilon)
                    && Outer1.TolerantEquals(other.Outer1, epsilon)
                    && Outer2.TolerantEquals(other.Outer2, epsilon);
            }
            else if (Inner1.TolerantEquals(other.Inner2, epsilon))
            {
                return Inner2.TolerantEquals(other.Outer1, epsilon)
                    && Outer1.TolerantEquals(other.Outer2, epsilon)
                    && Outer2.TolerantEquals(other.Inner1, epsilon);
            }
            else if (Inner1.TolerantEquals(other.Outer1, epsilon))
            {
                return Inner2.TolerantEquals(other.Outer2, epsilon)
                    && Outer1.TolerantEquals(other.Inner1, epsilon)
                    && Outer2.TolerantEquals(other.Inner2, epsilon);
            }
            else if (Inner1.TolerantEquals(other.Outer2, epsilon))
            {
                return Inner2.TolerantEquals(other.Inner1, epsilon)
                    && Outer1.TolerantEquals(other.Inner2, epsilon)
                    && Outer2.TolerantEquals(other.Outer1, epsilon);
            }

            return false;
        }
    }
}
