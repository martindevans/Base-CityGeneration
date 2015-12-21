using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Utilities.Extensions
{
    public static class IEnumerableVector2Extensions
    {
        public static IOrderedEnumerable<T> OrderByPoint<T>(this IEnumerable<T> items, Func<T, Vector2> pointSelector)
        {
            return items.OrderBy(a => pointSelector(a).X)
                        .ThenBy(a => pointSelector(a).Y);
        }

        public static IEnumerable<T> ThenByPoint<T>(this IOrderedEnumerable<T> items, Func<T, Vector2> pointSelector)
        {
            return items.ThenBy(a => pointSelector(a).X)
                        .ThenBy(a => pointSelector(a).Y);
        }
    }
}
