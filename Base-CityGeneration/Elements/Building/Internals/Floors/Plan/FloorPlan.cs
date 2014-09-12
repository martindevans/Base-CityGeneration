using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class FloorPlan
    {
        #region fields/properties
        private const float SCALE = 100000;

        private const float SAFE_DISTANCE = 0.01f;

        private bool _isFrozen = false;
        private readonly Clipper _clipper = new Clipper();

        private readonly Vector2[] _externalFootprint;
        public IEnumerable<Vector2> ExternalFootprint
        {
            get { return _externalFootprint; }
        }

        private readonly List<RoomPlan> _rooms = new List<RoomPlan>();
        public IEnumerable<RoomPlan> Rooms
        {
            get { return _rooms; }
        }

        private readonly NeighbourData _neighbourhood;
        #endregion

        public FloorPlan(Vector2[] footprint)
        {
            _externalFootprint = footprint;

            _neighbourhood = new NeighbourData(this);
        }

        public void Freeze()
        {
            _isFrozen = true;

            _neighbourhood.GenerateNeighbours();
        }

        private int _nextRoomId = 0;

        public IEnumerable<RoomPlan> AddRoom(IEnumerable<Vector2> roomFootprint, float wallThickness, IEnumerable<ScriptReference> scripts, bool split = false)
        {
            if (_isFrozen)
                throw new InvalidOperationException("Cannot add rooms to floorplan once it is frozen");

            var clipperRoomFootprint = roomFootprint.Select(ToPoint).ToList();

            if (Clipper.Orientation(clipperRoomFootprint))
                throw new ArgumentException("Room footprint must be clockwise wound");

            //Contain room inside floor
            _clipper.Clear();
            _clipper.AddPolygon(clipperRoomFootprint, PolyType.Subject);
            _clipper.AddPolygon(_externalFootprint.Select(ToPoint).ToList(), PolyType.Clip);
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            _clipper.Execute(ClipType.Intersection, solution);

            if (solution.Count > 1 && !split)
                return new RoomPlan[0];
            if (solution.Count == 0)
                return new RoomPlan[0];

            //Clip against other rooms
            if (_rooms.Count > 0)
            {
                _clipper.Clear();
                _clipper.AddPolygons(solution, PolyType.Subject);
                _clipper.AddPolygons(_rooms.Select(r => r.OuterFootprint.Select(ToPoint).ToList()).ToList(), PolyType.Clip);
                solution.Clear();
                _clipper.Execute(ClipType.Difference, solution);

                if (solution.Count > 1 && !split)
                    return new RoomPlan[0];
                if (solution.Count == 0)
                    return new RoomPlan[0];
            }

            var s = scripts.ToArray();

            List<RoomPlan> result = new List<RoomPlan>();
            foreach (var shape in solution)
            {
                _neighbourhood.Dirty = true;

                //Ensure shape is still clockwise wound
                if (Clipper.Orientation(shape))
                    shape.Reverse();

                var r = new RoomPlan(this, shape.Select(ToVector2).Shrink(SAFE_DISTANCE).ToArray(), wallThickness, s, _nextRoomId++);
                result.Add(r);
                _rooms.Add(r);
            }

            return result;
        }

        public IEnumerable<Neighbour> GetNeighbours(RoomPlan room)
        {
            _neighbourhood.GenerateNeighbours();
            return _neighbourhood[room];
        }

        #region static helpers
        private static IntPoint ToPoint(Vector2 v)
        {
            return new IntPoint((int)(v.X * SCALE), (int)(v.Y * SCALE));
        }

        private static Vector2 ToVector2(IntPoint v)
        {
            return new Vector2(v.X / SCALE, v.Y / SCALE);
        }
        #endregion

        public class Neighbour
        {
            /// <summary>
            /// The index of the edge on roomAB
            /// </summary>
            public uint EdgeIndexRoomAB { get; private set; }

            /// <summary>
            /// One of the rooms in a neighbourship pair (touching points A and B)
            /// </summary>
            public RoomPlan RoomAB { get; private set; }

            /// <summary>
            /// The index of the edge on roomCD
            /// </summary>
            public uint EdgeIndexRoomCD { get; private set; }

            /// <summary>
            /// One of the rooms in a neighbourship pair (touching points C and D)
            /// </summary>
            public RoomPlan RoomCD { get; private set; }

            /// <summary>
            /// The first point on the border of these two rooms
            /// </summary>
            public Vector2 A { get; private set; }

            /// <summary>
            /// The second point on the border of these two rooms
            /// </summary>
            public Vector2 B { get; private set; }

            /// <summary>
            /// The third point on the border of these two rooms
            /// </summary>
            public Vector2 C { get; private set; }

            /// <summary>
            /// The fourth point on the border of these two rooms
            /// </summary>
            public Vector2 D { get; private set; }

            /// <summary>
            /// Distance along Edge AB to point A
            /// </summary>
            public float At { get; private set; }

            /// <summary>
            /// Distance along Edge AB to point B
            /// </summary>
            public float Bt { get; private set; }

            /// <summary>
            /// Distance along Edge CD to point C
            /// </summary>
            public float Ct { get; private set; }

            /// <summary>
            /// Distance along Edge CD to point D
            /// </summary>
            public float Dt { get; private set; }

            public Neighbour(uint edgeAbIndex, RoomPlan ab, uint edgeCdIndex, RoomPlan cd, Vector2 a, float at, Vector2 b, float bt, Vector2 c, float ct, Vector2 d, float dt)
            {
                EdgeIndexRoomAB = edgeAbIndex;
                EdgeIndexRoomCD = edgeCdIndex;

                RoomAB = ab;
                RoomCD = cd;

                A = a;
                B = b;
                C = c;
                D = d;

                At = at;
                Bt = bt;
                Ct = ct;
                Dt = dt;
            }

            public RoomPlan Other(RoomPlan room)
            {
                if (RoomAB == room)
                    return RoomCD;
                else
                    return RoomAB;
            }

            public LineSegment2D Segment(RoomPlan room)
            {
                if (RoomAB == room)
                    return new LineSegment2D(A, B);
                else
                    return new LineSegment2D(C, D);
            }
        }
    }
}
