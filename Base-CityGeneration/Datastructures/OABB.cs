using System;
using System.Numerics;

namespace Base_CityGeneration.Datastructures
{
    public struct OABB
    {
        public readonly Vector2 Middle;
        public readonly float Rotation;
        public readonly Vector2 Extents;
        public readonly float Area;

        public OABB(Vector2 middle, float rotation, Vector2 extents)
        {
            Middle = middle;
            Rotation = rotation;
            Extents = extents;
            Area = extents.X * extents.Y * 4;
        }

        /// <summary>
        /// Get a vector pointing along the shortest axis (i.e. across the longest axis)
        /// </summary>
        /// <returns></returns>
        internal Vector2 SplitDirection()
        {
            var sin = (float)Math.Sin(Rotation);
            var cos = (float)Math.Cos(Rotation);

            return (Extents.X < Extents.Y) ? new Vector2(cos, sin) : new Vector2(sin, cos);
        }
    }
}
