using System;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures
{
    public struct Ray2D
    {
        public readonly Vector2 Point;
        public readonly Vector2 Direction;

        public Ray2D(Vector2 point, Vector2 direction)
        {
            Point = point;
            Direction = direction;
        }

        private static float Cross2D(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public Vector2? Intersection2D(Ray2D other, out float t)
        {
            var p = Point;
	        var r = Direction;

	        var q = other.Point;
	        var s = other.Direction;

	        var rxs = Cross2D(r, s);
	        var qmp = q - p;

            if (Math.Abs(rxs - 0) < float.Epsilon)
            {
                t = 0;
                return null;
            }

            t = Cross2D(qmp, s) / rxs;
	        return p + (t * r);
        }

        public Vector2? Intersection2D(Ray2D other)
        {
            float t;
            return Intersection2D(other, out t);
        }
    }
}
