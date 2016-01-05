using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Myre.Extensions;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Datastructures
{
    public static class SeparatingAxisTester
    {
        public static bool Intersects(IReadOnlyList<Vector2> convex1, IReadOnlyList<Vector2> convex2)
        {
            Contract.Requires(convex1 != null);
            Contract.Requires(convex2 != null);

            foreach (var edge in Edges(convex1).Concat(Edges(convex2)))
            {
                float a1, b1;
                Project(edge, convex1, out a1, out b1);

                float a2, b2;
                Project(edge, convex2, out a2, out b2);

                if (Math.Min(a1, b1) > Math.Max(a2, b2) || Math.Max(a1, b1) < Math.Min(a2, b2))
                    return false;
            }

            return true;    //Couldn't find a separating axis
        }

        public static bool Intersects(Rectangle r, IReadOnlyList<Vector2> convex2)
        {
            Contract.Requires(convex2 != null);

            var convex1 = new Vector2[]
            {
                new Vector2(r.Left, r.Bottom),
                new Vector2(r.Right, r.Bottom),
                new Vector2(r.Right, r.Top),
                new Vector2(r.Left, r.Top)
            };

            return Intersects(convex1, convex2);
        }

        private static void Project(Ray2 r, IEnumerable<Vector2> shape, out float min, out float max)
        {
            Contract.Requires(shape != null);

            min = float.MaxValue;
            max = float.MinValue;

            foreach (var projection in shape.Select(point => Vector2.Dot(point - r.Position, r.Direction)))
            {
                min = Math.Min(projection, min);
                max = Math.Max(projection, max);
            }
        }

        private static IEnumerable<Ray2> Edges(IReadOnlyList<Vector2> shape)
        {
            Contract.Requires(shape != null);

            for (var i = 0; i < shape.Count; i++)
            {
                var a = shape[i];
                var b = shape[(i + 1) % shape.Count];

                yield return new Ray2(a, Vector2.Normalize(b - a));
            }
        }
    }
}
