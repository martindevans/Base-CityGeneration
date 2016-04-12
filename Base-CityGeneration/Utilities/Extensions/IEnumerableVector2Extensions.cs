using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors.Geometry;

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
            Contract.Ensures(Contract.Result<IOrderedEnumerable<T>>() != null);

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
        public static IOrderedEnumerable<T> ThenByPoint<T>(this IOrderedEnumerable<T> items, Func<T, Vector2> pointSelector)
        {
            Contract.Requires(items != null);
            Contract.Requires(pointSelector != null);
            Contract.Ensures(Contract.Result<IOrderedEnumerable<T>>() != null);

            return items.ThenBy(a => pointSelector(a).X)
                        .ThenBy(a => pointSelector(a).Y);
        }

        /// <summary>
        /// Convert a list of points into a list of segments connecting the points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static IEnumerable<LineSegment2> Segments(this IEnumerable<Vector2> points)
        {
            Vector2? first = null;
            Vector2? previous = null;
            foreach (var point in points)
            {
                if (!first.HasValue)
                    first = point;

                if (previous.HasValue)
                    yield return new LineSegment2(previous.Value, point);
                previous = point;
            }

            if (previous.HasValue)
                yield return new LineSegment2(previous.Value, first.Value);
        }
    }
}
