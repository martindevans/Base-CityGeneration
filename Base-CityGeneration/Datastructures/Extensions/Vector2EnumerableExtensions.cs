using System;
using System.Numerics;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class Vector2EnumerableExtensions
    {
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
