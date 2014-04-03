
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

        public bool Intersects(Rectangle b)
        {
            return Left <= b.Right
                && b.Left <= Right
                && Bottom <= b.Top
                && b.Bottom <= Top;
        }

        public bool Contains(Vector2 point)
        {
            return Left < point.X
                   && Right >= point.X
                   && Bottom < point.Y
                   && Top >= point.Y;
        }

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
    }
}
