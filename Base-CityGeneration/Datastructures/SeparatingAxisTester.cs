using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Datastructures
{
    public static class SeparatingAxisTester
    {
        public static bool Intersects(Vector2[] convex1, Vector2[] convex2)
        {
            foreach (var edge in Edges(convex1).Append(Edges(convex2)))
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

        public static bool Intersects(RectangleF r, Vector2[] convex2)
        {
            var convex1 = new Vector2[]
            {
                new Vector2(r.Left, r.Top),
                new Vector2(r.Right, r.Top),
                new Vector2(r.Right, r.Bottom),
                new Vector2(r.Left, r.Bottom)
            };

            return Intersects(convex1, convex2);
        }

        private static void Project(Ray2D r, IEnumerable<Vector2> shape, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            foreach (var projection in shape.Select(point => Vector2.Dot(point - r.Start, r.Direction)))
            {
                min = Math.Min(projection, min);
                max = Math.Max(projection, max);
            }
        }

        private static IEnumerable<Ray2D> Edges(Vector2[] shape)
        {
            for (int i = 0; i < shape.Length; i++)
            {
                var a = shape[i];
                var b = shape[(i + 1) % shape.Length];

                yield return new Ray2D(a, Vector2.Normalize(b - a));
            }
        }

        private struct Ray2D
        {
            public readonly Vector2 Start;
            public readonly Vector2 Direction;

            public Ray2D(Vector2 start, Vector2 direction)
            {
                Start = start;
                Direction = direction;
            }
        }
    }
}
