using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using EpimetheusPlugins.Procedural.Utilities;
using JetBrains.Annotations;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Datastructures
{
    /// <summary>
    /// Object aligned bounding rectangle
    /// </summary>
    public struct OABR
    {
        /// <summary>
        /// Center of the bounding box
        /// </summary>
        public readonly Vector2 Middle;

        public readonly Vector2 Axis;

        private Vector2 Perpendicular
        {
            get { return -Axis.Perpendicular(); }
        }

        public readonly Vector2 Min;

        public readonly Vector2 Max;

        /// <summary>
        /// Total area of this OABR
        /// </summary>
        public readonly float Area;

        public OABR(Vector2 middle, Vector2 primaryAxis, Vector2 min, Vector2 max)
        {
            Middle = middle;
            Axis = primaryAxis;
            Min = min;
            Max = max;

            var sz = Max - Min;
            Area = sz.X * sz.Y;
        }

        [Pure]
        public bool Contains(Vector2 point)
        {
            return new BoundingRectangle(Min, Max).Contains(FromWorld(point));
        }

        /// <summary>
        /// Get a vector pointing along the shortest axis (i.e. across the longest axis)
        /// </summary>
        /// <returns></returns>
        [Pure]
        internal Vector2 SplitDirection()
        {
            var size = Max - Min;

            if (size.X > size.Y)
                return Axis;
            else
                return Perpendicular;
        }

        /// <summary>
        /// Generate a series of OABB fittings (in no particular order)
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        internal static IEnumerable<OABR> Fittings(IEnumerable<Vector2> shape)
        {
            //Finding the OABB of the hull is the same as finding the OABB of the parcel, but is quicker
            var hull = shape.ConvexHull().ToArray();

            //Find middle of hull
            var middle = hull.Aggregate(Vector2.Zero, (current, t) => current + t / hull.Length);

            //Move all the points to be around the origin
            for (var i = 0; i < hull.Length; i++)
                hull[i] -= middle;

            //Generate ordered list of all OABBs (each aligned with an edge of the convex hull)
            return Enumerable
                .Range(0, hull.Length)
                .Select(i => {
                    var a = hull[i];
                    var b = hull[(i + 1) % hull.Length];

                    //Calculate projection axes (along and across)
                    var primary = Vector2.Normalize(b - a);
                    var secondary = -primary.Perpendicular();

                    //Project points on axes and measure size
                    var min = new Vector2(float.PositiveInfinity);
                    var max = new Vector2(float.NegativeInfinity);
                    foreach (var vertex in hull)
                    {
                        var pd = new Ray2(Vector2.Zero, primary).ClosestPointDistanceAlongLine(vertex);
                        var sd = new Ray2(Vector2.Zero, secondary).ClosestPointDistanceAlongLine(vertex);

                        min = Vector2.Min(min, new Vector2(pd, sd));
                        max = Vector2.Max(max, new Vector2(pd, sd));
                    }

                    return new OABR(middle, primary, min, max);
                });
        }

        /// <summary>
        /// Fit the optimal OABB to this shape
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        public static OABR Fit(IEnumerable<Vector2> shape)
        {
            return Fittings(shape).Aggregate((a, b) => a.Area < b.Area ? a : b);
        }

        public void Points(out Vector2 a, out Vector2 b, out Vector2 c, out Vector2 d)
        {
            a = ToWorld(new Vector2(Max.X, Max.Y));
            b = ToWorld(new Vector2(Max.X, Min.Y));
            c = ToWorld(new Vector2(Min.X, Min.Y));
            d = ToWorld(new Vector2(Min.X, Max.Y));
        }

        /// <summary>
        /// Convert a point from OABR space to world space
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector2 ToWorld(Vector2 point)
        {
            return Middle
                + Axis * point.X
                + Perpendicular * point.Y;
        }

        /// <summary>
        /// Convert a point from world space to OABR space
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector2 FromWorld(Vector2 point)
        {
            //Transform the point back towards the middle
            point -= Middle;

            //Calculate projection axes (along and across)
            var primary = Axis;
            var secondary = Perpendicular;

            var pd = new Ray2(Vector2.Zero, primary).ClosestPointDistanceAlongLine(point);
            var sd = new Ray2(Vector2.Zero, secondary).ClosestPointDistanceAlongLine(point);

            return new Vector2(pd, sd);
        }

        public IList<Vector2> Points(IList<Vector2> output = null)
        {
            if (output == null)
                output = new Vector2[4];
            if (output.Count < 4)
                throw new ArgumentException("Output array is too small", "output");

            Vector2 a, b, c, d;
            Points(out a, out b, out c, out d);

            output[0] = a;
            output[1] = b;
            output[2] = c;
            output[3] = d;

            return output;
        }
    }
}
