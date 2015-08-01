﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
using FMOD;
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
            var orderedEdges = (from edge in _vertex.OrderedEdges()
                                select new NWayJunctionEdgeData(edge)).ToArray();

            //Extract points for pairs of edges (edge and previous, edge and next)
            for (int i = 0; i < orderedEdges.Length; i++)
            {
                var prev = orderedEdges[(i + orderedEdges.Length - 1) % orderedEdges.Length];
                var edge = orderedEdges[i];

                ExtractPoints(prev, edge);
            }

            //Extract junction shape
            return new ReadOnlyCollection<Vector2>(orderedEdges
                .SelectMany(e => e.AllPoints).Quickhull2D().ToArray()
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

            //Check if roads are totally parallel
            if (!lrIntersect.HasValue || !rlIntersect.HasValue || !rrIntersect.HasValue || !llIntersect.HasValue)
            {
                ExtractPointsFromParallelRoads(at, bt);
                return;
            }

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
            if (llIntersect.Value.DistanceAlongLineA > lrIntersect.Value.DistanceAlongLineA) {
                at.LeftEnd = lrIntersect.Value.Position;
                bt.RightEnd = lrIntersect.Value.Position;

            } else {

                at.LeftEnd = rlIntersect.Value.Position + new Vector2(-at.Direction.Y, at.Direction.X) * at.Width;
                bt.RightEnd = rlIntersect.Value.Position + new Vector2(bt.Direction.Y, -bt.Direction.X) * bt.Width;

                left.Curve = new CircleSegment
                {
                    CenterPoint = rlIntersect.Value.Position,
                    StartPoint = at.LeftEnd,
                    EndPoint = bt.RightEnd
                };
            }
        }

        private void ExtractPointsFromParallelRoads(IHalfEdgeBuilder at, IHalfEdgeBuilder bt)
        {
            if (at.Width.TolerantEquals(bt.Width, 0.01f)) {
                //Roads are totally parallel, have the same width, and join to the same vertex... a.k.a: a straight line
                var w = at.Width * 0.5f;
                var d = at.Direction.Perpendicular();
                var side = d * w;

                at.LeftEnd = _vertex.Position - side;
                //at.RightEnd = _vertex.Position + side;

                //bt.LeftEnd = at.RightEnd;
                bt.RightEnd = at.LeftEnd;
            } else {
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

        private class NWayJunctionEdgeData
        {
            public readonly IHalfEdgeBuilder Builder;

            public ICurve Curve;

            public IEnumerable<Vector2> AllPoints
            {
                get
                {
                    if (Curve != null)
                    {
                        foreach (var point in Curve.Evaluate(0.25f))
                        {
                            yield return point;
                        }
                    }
                    yield return Builder.RightEnd;
                    yield return Builder.LeftEnd;
                    
                }
            }

            public NWayJunctionEdgeData(IHalfEdgeBuilder builder)
            {
                Builder = builder;
            }
        }
    }
}
