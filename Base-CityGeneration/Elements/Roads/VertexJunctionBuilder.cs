using System;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;

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
                    return GenerateRoadJoin(_vertex.Edges.First(), _vertex.Edges.Skip(1).First());
                case 3:
                    return GenerateTJunction(_vertex.Edges.First(), _vertex.Edges.Skip(2).First(), _vertex.Edges.Skip(3).First());
                case 4:
                    return GenerateCrossroads(_vertex.Edges.First(), _vertex.Edges.Skip(2).First(), _vertex.Edges.Skip(3).First(), _vertex.Edges.Skip(4).First());
                default:
                    return GenerateNWayJunction();
            }
        }

        private Vector2[] GenerateDeadEnd(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a)
        {
            throw new NotImplementedException();
        }

        private Vector2[] GenerateRoadJoin(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> b)
        {
            var x = a.IsPrimaryEdge ? a.Tag : a.Pair.Tag;
            var xLeft = x.Left;
            var xRight = x.Right;

            var y = b.IsPrimaryEdge ? b.Tag : b.Pair.Tag;
            var yLeft = y.Left;
            var yRight = y.Right;

            var areParallel = !x.Left.Intersection2D(y.Left).HasValue;

            if (areParallel)
            {
                //Two roads, going in *exactly* the same direction come together.
                //Why is this even considered a goddamn junction!?
                throw new NotImplementedException();
            }
            else
            {
// We know these roads are not parallel, so these will all have a value
// ReSharper disable PossibleInvalidOperationException
                var xl = xLeft.Intersection2D(yRight).Value;
                var xr = xRight.Intersection2D(yRight).Value;
                if (x.HalfEdge.EndVertex.Equals(_vertex))
                {
                    x.LeftEnd = xl;
                    x.RightEnd = xr;
                }
                else
                {
                    x.LeftStart = xl;
                    x.RightStart = xr;
                }

                var yl = yLeft.Intersection2D(xRight).Value;
                var yr = yRight.Intersection2D(xRight).Value;
                if (y.HalfEdge.EndVertex.Equals(_vertex))
                {
                    y.LeftEnd = yl;
                    y.RightEnd = yr;
                }
                else
                {
                    y.LeftStart = yl;
                    y.RightStart = yr;
                }

                Vector2[] points =
                {
                    xl, xr, yl, yr, xLeft.Intersection2D(yLeft).Value
                };
// ReSharper restore PossibleInvalidOperationException

                return points.Quickhull2D().ToArray();
            }
        }

        private Vector2[] GenerateTJunction(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> b, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> c)
        {
            throw new NotImplementedException();
        }

        private Vector2[] GenerateCrossroads(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> a, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> b, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> c, HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> d)
        {
            throw new NotImplementedException();
        }

        private Vector2[] GenerateNWayJunction()
        {
            throw new NotImplementedException();
        }
    }
}
