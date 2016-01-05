using System.Collections.ObjectModel;
using Base_CityGeneration.Datastructures.HalfEdge;
using EpimetheusPlugins.Procedural.Utilities;
using System.Numerics;
using System.Linq;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Roads
{
    public class HalfEdgeRoadBuilder : IHalfEdgeBuilder
    {
        public HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> HalfEdge { get; private set; }
        private readonly float _laneWidth;

        public uint Lanes { get; private set; }

        private readonly Ray2 _left;
        public Ray2 Left { get { return _left; } }

        private readonly Ray2 _right;
        public Ray2 Right { get { return _right; } }

        public Vector2 LeftStart { get; set; }
        public Vector2 RightStart { get; set; }

        public Vector2 LeftEnd { get; set; }
        public Vector2 RightEnd { get; set; }

        public float Width
        {
            get { return _laneWidth * Lanes * 2 + SidewalkWidth * 2; }
        }

        public float SidewalkWidth { get; private set; }

        private ReadOnlyCollection<Vector2> _footprint;
        public ReadOnlyCollection<Vector2>  Shape
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

        public HalfEdgeRoadBuilder(HalfEdge<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> halfEdge, float laneWidth, float sidewalkWidth, uint roadLanes)
        {
            HalfEdge = halfEdge;
            _laneWidth = laneWidth;
            SidewalkWidth = sidewalkWidth;
            Lanes = roadLanes;

            var n = Direction.Perpendicular() * Width * 0.5f;
            _left = new Ray2(HalfEdge.EndVertex.Position - n, Direction);
            _right = new Ray2(HalfEdge.EndVertex.Position + n, Direction);
        }

        private ReadOnlyCollection<Vector2> CalculateShape()
        {
            var s = new Vector2[] { LeftStart, LeftEnd, RightEnd, RightStart };
            return new ReadOnlyCollection<Vector2>(s.ConvexHull().ToArray());
        }
    }
}
