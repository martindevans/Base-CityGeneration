
using System;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures
{
    public struct Rectangle
    {
        public readonly float Bottom;
        public readonly float Left;
        public readonly float Width;
        public readonly float Height;

        public float Right
        {
            get { return Left + Width; }
        }

        public float Top
        {
            get { return Bottom + Height; }
        }

        public Vector2 TopLeft
        {
            get { return new Vector2(Left, Bottom); }
        }

        public Vector2 TopRight
        {
            get { return new Vector2(Right, Bottom); }
        }

        public Vector2 BottomLeft
        {
            get { return new Vector2(Left, Top); }
        }

        public Vector2 BottomRight
        {
            get { return new Vector2(Right, Top); }
        }

        public Rectangle(float left, float bottom, float width, float height)
        {
            Bottom = bottom;
            Left = left;
            Width = width;
            Height = height;
        }

        [Pure]
        public bool Intersects(Rectangle b)
        {
            return Left <= b.Right
                && b.Left <= Right
                && Bottom <= b.Top
                && b.Bottom <= Top;
        }

        [Pure]
        public bool Contains(Vector2 point)
        {
            return Left < point.X
                   && Right >= point.X
                   && Bottom < point.Y
                   && Top >= point.Y;
        }

        [Pure]
        public Vector2[] ToConvex()
        {
            return new Vector2[]
            {
                new Vector2(Left, Bottom),
                new Vector2(Right, Bottom),
                new Vector2(Right, Top),
                new Vector2(Left, Top)
            };
        }

        public static Rectangle FromPoints(Vector2[] points)
        {
            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);
            for (int i = 0; i < points.Length; i++)
            {
                min = new Vector2(
                    Math.Min(min.X, points[i].X),
                    Math.Min(min.Y, points[i].Y)
                );

                max = new Vector2(
                    Math.Max(max.X, points[i].X),
                    Math.Max(max.Y, points[i].Y)
                );
            }

            return new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
        }
    }
}
