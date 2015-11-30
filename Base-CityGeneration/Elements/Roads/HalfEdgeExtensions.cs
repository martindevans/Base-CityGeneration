using System.Collections.ObjectModel;
using Base_CityGeneration.Datastructures.HalfEdge;
using System.Numerics;
using System;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Roads
{
    internal static class HalfEdgeExtensions
    {
        /// <summary>
        /// Get the builder for the edge which *ends* with this vertex
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="ending"></param>
        /// <returns></returns>
        public static IHalfEdgeBuilder BuilderEndingWith(this HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> edge, Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> ending)
        {
            if (edge.IsPrimaryEdge)
            {
                if (edge.EndVertex.Equals(ending))
                    return edge.Tag;
                else if (edge.Pair.EndVertex.Equals(ending))
                    return new Switcharoo(edge.Tag);
            }
            else
            {
                if (edge.EndVertex.Equals(ending))
                    return new Switcharoo(edge.Pair.Tag);
                else if (edge.Pair.EndVertex.Equals(ending))
                    return edge.Pair.Tag;
            }

            throw new ArgumentException("Edge does not start or end with given vertex", "ending");
        }

        /// <summary>
        /// Given a tag for a half edge, switch around and masquerade as the tag for the paired half edge
        /// </summary>
        private class Switcharoo
            : IHalfEdgeBuilder
        {
            private readonly IHalfEdgeBuilder _tag;

            public Switcharoo(IHalfEdgeBuilder tag)
            {
                _tag = tag;

                var n = Direction.Perpendicular() * Width * 0.5f;
                _left = new Ray2(HalfEdge.EndVertex.Position - n, Direction);
                _right = new Ray2(HalfEdge.EndVertex.Position + n, Direction);
            }

            public HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge
            {
                get { return _tag.HalfEdge.Pair; }
            }

            public ReadOnlyCollection<Vector2> Shape
            {
                get { return _tag.Shape; }
            }

            private readonly Ray2 _left;
            public Ray2 Left { get { return _left; } }

            private readonly Ray2 _right;
            public Ray2 Right { get { return _right; } }

            public float Width
            {
                get { return _tag.Width; }
            }

            public float SidewalkWidth
            {
                get
                {
                    return _tag.SidewalkWidth;
                }
            }

            public Vector2 LeftStart
            {
                get
                {
                    return _tag.RightEnd;
                }
                set
                {
                    _tag.RightEnd = value;
                }
            }

            public Vector2 RightStart
            {
                get
                {
                    return _tag.LeftEnd;
                }
                set
                {
                    _tag.LeftEnd = value;
                }
            }

            public Vector2 LeftEnd
            {
                get
                {
                    return _tag.RightStart;
                }
                set
                {
                    _tag.RightStart = value;
                }
            }

            public Vector2 RightEnd
            {
                get
                {
                    return _tag.LeftStart;
                }
                set
                {
                    _tag.LeftStart = value;
                }
            }

            public Vector2 Direction
            {
                get
                {
                    return -_tag.Direction;
                }
            }
        }
    }
}
