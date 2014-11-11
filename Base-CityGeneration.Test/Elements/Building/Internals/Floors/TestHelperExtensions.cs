using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors
{
    public static class TestHelperExtensions
    {
        public static bool RoughlyContains(this IEnumerable<Vector2> points, Vector2 point, float epsilon)
        {
            return points.Any(a => Vector2.Distance(a, point) <= epsilon);
        }
    }
}
