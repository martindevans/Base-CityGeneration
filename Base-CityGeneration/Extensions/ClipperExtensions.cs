using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClipperLib;

namespace Base_CityGeneration.Extensions
{
    public static class ClipperExtensions
    {
        /// <summary>
        /// Calculate the intersection of all given shapes
        /// </summary>
        /// <param name="clipper"></param>
        /// <param name="shapes"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<Vector2>> IntersectAll(this Clipper clipper, IEnumerable<IEnumerable<Vector2>> shapes, int scale = 1000)
        {
            //Just in case someone else made a mess, clean up before we start
            clipper.Clear();

            //First shape is taken as the first "previous"
            var previous = new List<List<IntPoint>> {
                shapes.First().Select(a => new IntPoint(a.X * scale, a.Y * scale)).ToList()
            };

            //Loop through each shape (except the first) intersecting reuslts of all previous intersections with this shape
            foreach (var shape in shapes.Skip(1))
            {
                clipper.AddPaths(previous, PolyType.ptSubject, true);
                clipper.AddPath(shape.Select(a => new IntPoint(a.X * scale, a.Y * scale)).ToList(), PolyType.ptClip, true);

                clipper.Execute(ClipType.ctIntersection, previous);
                clipper.Clear();
            }

            //Convert back into normal scale
            return previous.Select(s => s.Select(v => new Vector2((float)v.X / scale, (float)v.Y / scale)));
        }
    }
}
