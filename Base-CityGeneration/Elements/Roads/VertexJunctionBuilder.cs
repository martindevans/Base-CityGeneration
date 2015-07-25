using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private ReadOnlyCollection<Vector2> _footprint = null;
        public ReadOnlyCollection<Vector2> Shape
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

        private ReadOnlyCollection<Vector2> CalculateShape()
        {
            switch (_vertex.Edges.Count())
            {
                case 1:
                    return GenerateDeadEnd(_vertex.Edges.Single());
                case 2:
                    return Generate2Way(_vertex.Edges.First(), _vertex.Edges.Skip(1).First());
                default:
                    return GenerateNWayJunction();
            }
        }

        private ReadOnlyCollection<Vector2> GenerateDeadEnd(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a)
        {
            //Get the builder for the edge ending at _vertex
            var b = a.BuilderEndingWith(_vertex);

            var ld = Geometry2D.ClosestPointDistanceAlongLine(b.Left, _vertex.Position);
            b.LeftEnd = b.Left.Point + b.Left.Direction * ld;

            var rd = Geometry2D.ClosestPointDistanceAlongLine(b.Right, _vertex.Position);
            b.RightEnd = b.Right.Point + b.Right.Direction * rd;

            //Dead ends do not create *any* junction, so return null for the junction shape
            return null;
        }

        private ReadOnlyCollection<Vector2> Generate2Way(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> b)
        {
            var att = new NWayJunctionEdgeData(a.BuilderEndingWith(_vertex));
            var btt = new NWayJunctionEdgeData(b.BuilderEndingWith(_vertex));

            ExtractPoints(att, btt);
            ExtractPoints(btt, att);

            return new ReadOnlyCollection<Vector2>(
                att.AllPoints.Append(btt.AllPoints)
                   .Quickhull2D()
                   .ToArray()
            );
        }

        private ReadOnlyCollection<Vector2> GenerateNWayJunction()
        {
            //Order the edges by their angle around the vertex
            var orderedEdges = (from edge in _vertex.Edges
                                let b = edge.BuilderEndingWith(_vertex)
                                let angle = (float)Math.Atan2(b.Direction.Y, b.Direction.X)
                                orderby angle descending
                                select new NWayJunctionEdgeData(b)).ToArray();

            //Extract points for pairs of edges (edge and previous, edge and next)
            for (int i = 0; i < orderedEdges.Length; i++)
            {
                var prev = orderedEdges[(i + orderedEdges.Length - 1) % orderedEdges.Length];
                var edge = orderedEdges[i];
                //var next = orderedEdges[(i + 1) % orderedEdges.Length];

                ExtractPoints(prev, edge);
                //ExtractPoints(edge, next);
            }

            //Extract junction shape (convex hull of all points generated above)
            return new ReadOnlyCollection<Vector2>(orderedEdges
                .SelectMany(e => e.AllPoints).ToArray()
                .Quickhull2D().ToArray()
            );
        }

        private void ExtractPoints(NWayJunctionEdgeData right, NWayJunctionEdgeData left)
        {
            var at = right.Builder;
            var bt = left.Builder;

            //Find intersection points between both sides of both roads
            var lrIntersect = Geometry2D.LineLineIntersection(at.Left, bt.Right);
            var rlIntersect = Geometry2D.LineLineIntersection(at.Right, bt.Left);
            var rrIntersect = Geometry2D.LineLineIntersection(at.Right, bt.Right);
            var llIntersect = Geometry2D.LineLineIntersection(at.Left, bt.Left);

            if (!lrIntersect.HasValue || !rlIntersect.HasValue || !rrIntersect.HasValue || !llIntersect.HasValue)
            {
                //Roads are totally parallel
                if (at.Width.TolerantEquals(bt.Width, 0.01f))
                {
                    //Roads are totally parallel, have the same width, and join to the same vertex... a.k.a: a straight line
                    var w = at.Width * 0.5f;
                    var d = at.Direction.Perpendicular();
                    var side = d * w;

                    at.LeftEnd = _vertex.Position - side;
                    //at.RightEnd = _vertex.Position + side;

                    //bt.LeftEnd = at.RightEnd;
                    bt.RightEnd = at.LeftEnd;
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
                    //at.RightEnd = at.HalfEdge.Pair.EndVertex.Position + aAlong + aSide;

                    var bAlong = bdn * (bl - Math.Min(0.9f * bl, widthDif * 1.5f));
                    var bSide = bdn.Perpendicular() * bt.Width * 0.5f;
                    //bt.LeftEnd = bt.HalfEdge.Pair.EndVertex.Position + bAlong - bSide;
                    bt.RightEnd = bt.HalfEdge.Pair.EndVertex.Position + bAlong + bSide;
                }
            }
            else
            {
                //Roads are not parallel

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
                    //at.RightEnd = rrIntersect.Value.Position;
                    //bt.LeftEnd = llIntersect.Value.Position;
                    bt.RightEnd = lrIntersect.Value.Position;

                    //point is the point of the junction
                    //xSide is the point on road x nearest the point
                    //aPoint = rlIntersect.Value.Position;
                    //aSide = at.RightEnd;
                    //bPoint = rlIntersect.Value.Position;
                    //bSide = bt.LeftEnd;

                    return;
                }
                else
                {
                    at.LeftEnd = llIntersect.Value.Position;
                    //at.RightEnd = rlIntersect.Value.Position;
                    //bt.LeftEnd = rlIntersect.Value.Position;
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

                right.Point = aPoint;

                var bEndToPoint = bPoint - bSide;
                var bEndToPointDist = bEndToPoint.Length();
                if (bEndToPointDist > maxWidth)
                    bPoint = bSide + (bEndToPoint / bEndToPointDist * maxWidth);

                left.Point = bPoint;

                ////Junction surrounds all these points
                //Vector2[] points = { at.LeftEnd, at.RightEnd, bt.LeftEnd, bt.RightEnd, aPoint, bPoint };
                //return points.Quickhull2D().ToArray();
            }
        }

        private class NWayJunctionEdgeData
        {
            public readonly IHalfEdgeBuilder Builder;

            public Vector2? Point;

            public IEnumerable<Vector2> AllPoints
            {
                get
                {
                    yield return Builder.LeftEnd;
                    yield return Builder.RightEnd;
                    if (Point.HasValue)
                        yield return Point.Value;
                }
            }

            public NWayJunctionEdgeData(IHalfEdgeBuilder builder)
            {
                Builder = builder;
            }
        }
    }
}
