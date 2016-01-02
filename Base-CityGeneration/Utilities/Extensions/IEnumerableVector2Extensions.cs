using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Utilities.Extensions
{
    public static class IEnumerableVector2Extensions
    {
        /// <summary>
        /// Order by points in a deterministic but arbitrary way
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="pointSelector"></param>
        /// <returns></returns>
        public static IOrderedEnumerable<T> OrderByPoint<T>(this IEnumerable<T> items, Func<T, Vector2> pointSelector)
        {
            Contract.Requires(items != null);
            Contract.Requires(pointSelector != null);

            return items.OrderBy(a => pointSelector(a).X)
                        .ThenBy(a => pointSelector(a).Y);
        }

        /// <summary>
        /// Order by points in a deterministic but arbitrary way
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="pointSelector"></param>
        /// <returns></returns>
        public static IEnumerable<T> ThenByPoint<T>(this IOrderedEnumerable<T> items, Func<T, Vector2> pointSelector)
        {
            Contract.Requires(items != null);
            Contract.Requires(pointSelector != null);

            return items.ThenBy(a => pointSelector(a).X)
                        .ThenBy(a => pointSelector(a).Y);
        }
    }
}
