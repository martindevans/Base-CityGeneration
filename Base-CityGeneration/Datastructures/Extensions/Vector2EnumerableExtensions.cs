using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class Vector2EnumerableExtensions
    {
        public static OABB FitOabb(this IEnumerable<Vector2> points)
        {
            Random r = new Random();
            return points.FitOabb(0, 0, r.NextDouble);
        }

        public static OABB FitOabb(this IEnumerable<Vector2> points, float nonOptimalityChance, float maximumNonOptimalityRatio, Func<double> random)
        {
            //Finding the OABB of the hull is the same as finding the OABB of the parcel, but is quicker
            var hull = points.Quickhull2D().ToArray();

            //Find middle of hull
            Vector2 middle = hull.Aggregate(Vector2.Zero, (current, t) => current + t / hull.Length);

            //Generate ordered list of all OABBs (each aligned with an edge of the convex hull)
            var oabbs = Enumerable
                .Range(0, hull.Length)
                .Select(i =>
                {
                    var a = hull[i];
                    var b = hull[(i + 1) % hull.Length];

                    //Get the angle of this edge from the vertical
                    var angle = (float)Math.Atan2(b.X - a.X, b.Y - a.Y);

                    //Find the bounding box for this orientation
                    Vector2 min = new Vector2(float.MaxValue);
                    Vector2 max = new Vector2(float.MinValue);
                    foreach (var rotated in hull.Select(v => RotateAround(v, middle, -angle)))
                    {
                        min.X = Math.Min(min.X, rotated.X);
                        min.Y = Math.Min(min.Y, rotated.Y);
                        max.X = Math.Max(max.X, rotated.X);
                        max.Y = Math.Max(max.Y, rotated.Y);
                    }

                    var extents = (max - min) / 2;
                    return new OABB(middle, -angle, extents);
                })
                .OrderBy(a => a.Extents.X * a.Extents.Y * 4).ToArray();

            //Now select an OABB from this list, with the first (smallest) being the most likely
            int selected = 0;
            for (int i = 1; i < oabbs.Length; i++)
            {
                //Do not allow the area ratio between optimal and selected to go over the non optimality limit
                if (oabbs[i].Area / (oabbs[0]).Area > maximumNonOptimalityRatio)
                    break;

                //We're not breaking the non optimality limit, so what's the chance of selecting this?
                var chance = Math.Pow(nonOptimalityChance, i);
                selected = random() < chance ? i : selected;
            }

            return oabbs[selected];
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
    }
}
