using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Roads
{
    public class HalfEdgeRoadBuilder : IHalfEdgeBuilder
    {
        public HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge { get; private set; }
        private readonly float _laneWidth;
        private readonly float _sidewalkWidth;

        public int Lanes { get; private set; }

        private readonly Line2D _left;
        public Line2D Left { get { return _left; } }

        private readonly Line2D _right;
        public Line2D Right { get { return _right; } }

        public Vector2 LeftStart { get; set; }
        public Vector2 RightStart { get; set; }

        public Vector2 LeftEnd { get; set; }
        public Vector2 RightEnd { get; set; }

        public float Width
        {
            get { return _laneWidth * Lanes * 2 + _sidewalkWidth * 2; }
        }

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

        public Vector2 Direction
        {
            get { return Vector2.Normalize(HalfEdge.EndVertex.Position - HalfEdge.Pair.EndVertex.Position); }
        }

        public Vector2 LeftProjection;
        public Vector2 RightProjection;

        public HalfEdgeRoadBuilder(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> halfEdge, float laneWidth, float sidewalkWidth, int roadLanes)
        {
            HalfEdge = halfEdge;
            _laneWidth = laneWidth;
            _sidewalkWidth = sidewalkWidth;
            Lanes = roadLanes;

            var n = Direction.Perpendicular() * Width * 0.5f;
            _right = new Line2D(HalfEdge.EndVertex.Position - n, Direction);
            _left = new Line2D(HalfEdge.EndVertex.Position + n, Direction);
        }

        private Vector2[] CalculateShape()
        {
            var s = new Vector2[] { LeftStart, LeftEnd, RightEnd, RightStart };
            return s.Quickhull2D().ToArray();
        }
    }
}
