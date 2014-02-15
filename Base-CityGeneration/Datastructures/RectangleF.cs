
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures
{
    public struct RectangleF
    {
        public readonly float Top;
        public readonly float Left;
        public readonly float Width;
        public readonly float Height;

        public float Right
        {
            get { return Left + Width; }
        }

        public float Bottom
        {
            get { return Top + Height; }
        }

        public Vector2 TopLeft
        {
            get { return new Vector2(Left, Top); }
        }

        public Vector2 TopRight
        {
            get { return new Vector2(Right, Top); }
        }

        public Vector2 BottomLeft
        {
            get { return new Vector2(Left, Bottom); }
        }

        public Vector2 BottomRight
        {
            get { return new Vector2(Right, Bottom); }
        }

        public RectangleF(float left, float top, float width, float height)
        {
            Top = top;
            Left = left;
            Width = width;
            Height = height;
        }

        public bool Intersects(RectangleF b)
        {
            return Left <= b.Right
                && b.Left <= Right
                && Top <= b.Bottom
                && b.Top <= Bottom;
        }

        public bool Contains(Vector2 point)
        {
            return Left < point.X
                   && Right >= point.X
                   && Top < point.Y
                   && Bottom >= point.Y;
        }

        public Vector2[] ToConvex()
        {
            return new Vector2[]
            {
                new Vector2(Left, Top),
                new Vector2(Right, Top),
                new Vector2(Right, Bottom),
                new Vector2(Left, Bottom)
            };
        }
    }
}
