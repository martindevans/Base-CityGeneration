using System.Linq;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Datastructures.HalfEdge;
using Microsoft.Xna.Framework;
using Myre.Extensions;
using Placeholder.ConstructiveSolidGeometry;

namespace Base_CityGeneration.Elements.Roads
{
    public class HalfEdgeRoadBuilder : IHalfEdgeBuilder
    {
        public HalfEdge HalfEdge { get; private set; }
        private readonly float _laneWidth;
        private readonly float _sidewalkWidth;

        public int Lanes { get; private set; }

        private Ray2D? _left;
        public Ray2D Left
        {
            get
            {
                if (!_left.HasValue)
                {
                    var rNormal = Direction.Perpendicular() * Width * 0.5f;
                    _left = new Ray2D(HalfEdge.EndVertex.Position - rNormal, Direction);
                }
                return _left.Value;
            }
        }

        private Ray2D? _right;
        public Ray2D Right
        {
            get
            {
                if (!_right.HasValue)
                {
                    var rNormal = Direction.Perpendicular() * Width;
                    _right = new Ray2D(HalfEdge.EndVertex.Position + rNormal, Direction);
                }
                return _right.Value;
            }
        }

        public Vector2 LeftStart { get; set; }
        public Vector2 RightStart { get; set; }

        public Vector2 LeftEnd { get; set; }
        public Vector2 RightEnd { get; set; }

        public float Width
        {
            get { return _laneWidth * Lanes + _sidewalkWidth * 2; }
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

        public HalfEdgeRoadBuilder(HalfEdge halfEdge, float laneWidth, float sidewalkWidth, int roadLanes)
        {
            HalfEdge = halfEdge;
            _laneWidth = laneWidth;
            _sidewalkWidth = sidewalkWidth;
            Lanes = roadLanes;
        }

        private Vector2[] CalculateShape()
        {
            var s = new Vector2[] { LeftStart, LeftEnd, RightEnd, RightStart };
            return s.Quickhull2D().ToArray();
        }

        public Ray2D GetLeftSide(bool primary)
        {
            return primary ? Left : Right;
        }

        public Ray2D GetRightSide(bool primary)
        {
            return primary ? Right : Left;
        }
    }
}
