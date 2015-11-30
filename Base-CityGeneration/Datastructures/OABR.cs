using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using Vector2 = System.Numerics.Vector2;

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

        /// <summary>
        /// Rotation from BB -> World space
        /// </summary>
        public readonly float Rotation;

        /// <summary>
        /// Size of the bounding rectangle
        /// </summary>
        public readonly Vector2 Extents;
        public readonly float Area;

        public OABR(Vector2 middle, float rotation, Vector2 extents)
        {
            Middle = middle;
            Rotation = rotation;
            Extents = extents;
            Area = extents.X * extents.Y * 4;
        }

        /// <summary>
        /// Get a vector pointing along the shortest axis (i.e. across the longest axis)
        /// </summary>
        /// <returns></returns>
        internal Vector2 SplitDirection()
        {
            var sin = (float)Math.Sin(Rotation);
            var cos = (float)Math.Cos(Rotation);

            return (Extents.X < Extents.Y) ? new Vector2(cos, sin) : new Vector2(sin, cos);
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

            //Generate ordered list of all OABBs (each aligned with an edge of the convex hull)
            return Enumerable
                .Range(0, hull.Length)
                .Select(i => {
                    var a = hull[i];
                    var b = hull[(i + 1) % hull.Length];

                    //Get the angle of this edge from the vertical
                    var angle = (float)Math.Atan2(b.X - a.X, b.Y - a.Y);

                    //Find the bounding box for this orientation
                    var min = new Vector2(float.MaxValue);
                    var max = new Vector2(float.MinValue);
                    foreach (var rotated in hull.Select(v => RotateAround(v, middle, -angle)))
                    {
                        min = Vector2.Min(min, rotated);
                        max = Vector2.Max(max, rotated);
                    }

                    var extents = (max - min) / 2;
                    return new OABR(middle, angle, extents);
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

        private static Vector2 RotateAround(Vector2 point, Vector2 origin, float angle)
        {
            var c = (float)Math.Cos(angle);
            var s = (float)Math.Sin(angle);

            return new Vector2(
                c * (point.X - origin.X) - s * (point.Y - origin.Y) + origin.X,
                s * (point.X - origin.X) + c * (point.Y - origin.Y) + origin.Y
            );
        }

        public void Points(out Vector2 a, out Vector2 b, out Vector2 c, out Vector2 d)
        {
            a = RotateAround(Middle + Extents * new Vector2(-1, 1), Middle, Rotation);
            b = RotateAround(Middle + Extents * new Vector2(1, 1), Middle, Rotation);
            c = RotateAround(Middle + Extents * new Vector2(1, -1), Middle, Rotation);
            d = RotateAround(Middle + Extents * new Vector2(-1, -1), Middle, Rotation);
        }

        public IList<Vector2> Points(IList<Vector2> output)
        {
            if (output == null)
                throw new ArgumentNullException("output");
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
