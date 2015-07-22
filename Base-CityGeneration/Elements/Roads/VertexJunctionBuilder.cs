using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;
using Myre.Extensions;
using System;
using System.Linq;

namespace Base_CityGeneration.Elements.Roads
{
    public class VertexJunctionBuilder
        :IVertexBuilder
    {
        private readonly Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> _vertex;

        private Vector2[] _footprint = null;
        public Vector2[] Shape
        {
            get
            {
                if (_footprint == null)
                    _footprint = CalculateShape();
                return _footprint;
            }
        }

        public VertexJunctionBuilder(Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> vertex)
        {
            _vertex = vertex;
        }

        private Vector2[] CalculateShape()
        {
            switch (_vertex.Edges.Count())
            {
                case 1:
                    return GenerateDeadEnd(_vertex.Edges.Single());
                case 2:
                    //return GenerateRoadJoin(_vertex.Edges.First(), _vertex.Edges.Skip(1).First());
                    return Generate2Way(_vertex.Edges.First(), _vertex.Edges.Skip(1).First());
                default:
                    return GenerateNWayJunction();
            }
        }

        private Vector2[] GenerateDeadEnd(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a)
        {
            //Get the builder for the edge ending at _vertex
            var b = BuilderFor(a);

            var ld = Geometry2D.ClosestPointDistanceAlongLine(b.Left, _vertex.Position);
            b.LeftEnd = b.Left.Point + b.Left.Direction * ld;

            var rd = Geometry2D.ClosestPointDistanceAlongLine(b.Right, _vertex.Position);
            b.RightEnd = b.Right.Point + b.Right.Direction * rd;

            //Dead ends do not create *any* junction, so return null for the junction shape
            return null;
        }

        private Vector2[] Generate2Way(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> b)
        {
            var at = BuilderFor(a);
            var bt = BuilderFor(b);

            if (!Geometry2D.LineLineIntersection(at.Left, bt.Right).HasValue)
            {
                //Roads are totally parallel
                if (at.Width.TolerantEquals(bt.Width, 0.01f))
                {
                    //Roads are totally parallel, have the same width, and join to the same vertex... a.k.a: a straight line
                    var w = at.Width * 0.5f;
                    var d = Vector2.Normalize(_vertex.Position - at.HalfEdge.Pair.EndVertex.Position).Perpendicular();
                    var side = d * w;

                    at.LeftEnd = _vertex.Position - side;
                    at.RightEnd = _vertex.Position + side;

                    bt.LeftEnd = at.LeftEnd;
                    bt.RightEnd = at.RightEnd;

                    //No junction required
                    return null;
                }
                else
                {
                    //Roads are totally parallel, but have different widths
                    var ad = _vertex.Position - at.HalfEdge.Pair.EndVertex.Position;
                    var al = ad.Length();
                    var adn = ad / al;

                    var bd = _vertex.Position - bt.HalfEdge.Pair.EndVertex.Position;
                    var bl = bd.Length();
                    var bdn = bd / bl;

                    //What's the difference in widths?
                    var widthDif = Math.Abs(at.Width - bt.Width);

                    //Calculate a ramped junction shape from some distance along this roads segment (dependent upon width delta)
                    var aAlong = adn * (al - Math.Min(0.9f * al, widthDif * 1.5f));
                    var aSide = adn.Perpendicular() * at.Width * 0.5f;
                    at.LeftEnd = at.HalfEdge.Pair.EndVertex.Position + aAlong - aSide;
                    at.RightEnd = at.HalfEdge.Pair.EndVertex.Position + aAlong + aSide;

                    var bAlong = bdn * (bl - Math.Min(0.9f * bl, widthDif * 1.5f));
                    var bSide = bdn.Perpendicular() * bt.Width * 0.5f;
                    bt.LeftEnd = bt.HalfEdge.Pair.EndVertex.Position + bAlong - bSide;
                    bt.RightEnd = bt.HalfEdge.Pair.EndVertex.Position + bAlong + bSide;

                    //Junction shape fits around these 4 points
                    Vector2[] points = { at.LeftEnd, at.RightEnd, bt.LeftEnd, bt.RightEnd };
                    return points.Quickhull2D().ToArray();
                }
            }
            else
            {
                //Roads are not parallel

                //Find intersection points between both sides of both roads
                var lrIntersect = Geometry2D.LineLineIntersection(at.Left, bt.Right);
                var rlIntersect = Geometry2D.LineLineIntersection(at.Right, bt.Left);
                var rrIntersect = Geometry2D.LineLineIntersection(at.Right, bt.Right);
                var llIntersect = Geometry2D.LineLineIntersection(at.Left, bt.Left);

                //Sanity check
                if (!lrIntersect.HasValue || !rlIntersect.HasValue || !rrIntersect.HasValue || !llIntersect.HasValue)
                    throw new InvalidOperationException("Failed to find interesection for non parallel road segments");

                //there are two configurations for which sides are matched, depending on the directions the roads meet
                //
                // ---x---x    x---x---
                // B  |   |    |   |  B
                // ---x---x    x---x---
                //    | A |    | A |
                //
                // We can determine which configuration we're in by the distance along the edges the intersections lie at
                // Left config: llt > lrt
                // Right config: llt < lrt

                //Assign road positions, and pass out data about the point of the junction in these variables
                Vector2 aPoint, aSide, bPoint, bSide;
                if (llIntersect.Value.DistanceAlongLineA > lrIntersect.Value.DistanceAlongLineA)
                {
                    at.LeftEnd = lrIntersect.Value.Position;
                    at.RightEnd = rrIntersect.Value.Position;
                    bt.LeftEnd = llIntersect.Value.Position;
                    bt.RightEnd = lrIntersect.Value.Position;

                    //point is the point of the junction
                    //xSide is the point on road x nearest the point
                    aPoint = rlIntersect.Value.Position;
                    aSide = at.RightEnd;
                    bPoint = rlIntersect.Value.Position;
                    bSide = bt.LeftEnd;
                }
                else
                {
                    at.LeftEnd = llIntersect.Value.Position;
                    at.RightEnd = rlIntersect.Value.Position;
                    bt.LeftEnd = rlIntersect.Value.Position;
                    bt.RightEnd = rrIntersect.Value.Position;

                    aPoint = lrIntersect.Value.Position;
                    aSide = at.LeftEnd;
                    bPoint = lrIntersect.Value.Position;
                    bSide = bt.RightEnd;
                }

                //The length of the junction is dependent on the road widths
                var maxWidth = Math.Max(at.Width, bt.Width);

                //The "point" of the junction is located at the final intersection
                //However near parallel roads would results in a absurdly long point, limit the distance
                var aEndToPoint = aPoint - aSide;
                var aEndToPointDist = aEndToPoint.Length();
                if (aEndToPointDist > maxWidth)
                    aPoint = aSide + (aEndToPoint / aEndToPointDist * maxWidth);

                var bEndToPoint = bPoint - bSide;
                var bEndToPointDist = bEndToPoint.Length();
                if (bEndToPointDist > maxWidth)
                    bPoint = bSide + (bEndToPoint / bEndToPointDist * maxWidth);

                //Junction surrounds all these points
                Vector2[] points = { at.LeftEnd, at.RightEnd, bt.LeftEnd, bt.RightEnd, aPoint, bPoint };
                return points.Quickhull2D().ToArray();
            }
        }

        private Vector2[] GenerateNWayJunction()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the builder for the edge which *ends* with this vertex
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        private IHalfEdgeBuilder BuilderFor(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> edge)
        {
            if (edge.IsPrimaryEdge)
            {
                if (edge.EndVertex.Equals(_vertex))
                    return edge.Tag;
                else
                    return new Switcharoo(edge.Tag);
            }
            else
            {
                if (edge.EndVertex.Equals(_vertex))
                    return new Switcharoo(edge.Pair.Tag);
                else
                    return edge.Pair.Tag;
            }
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
                _right = new Line2D(HalfEdge.EndVertex.Position - n, Direction);
                _left = new Line2D(HalfEdge.EndVertex.Position + n, Direction);
            }

            public HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge
            {
                get { return _tag.HalfEdge.Pair; }
            }

            public Vector2[] Shape
            {
                get { return _tag.Shape; }
            }

            private readonly Line2D _left;
            public Line2D Left { get { return _left; } }

            private readonly Line2D _right;
            public Line2D Right { get { return _right; } }

            public float Width
            {
                get { return _tag.Width; }
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
