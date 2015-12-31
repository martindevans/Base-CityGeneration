using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using System.Numerics;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class FloorPlan
    {
        #region fields/properties
        private const float SCALE = 100000;

        private const float SAFE_DISTANCE = 0.01f;

        private bool _isFrozen = false;
        private readonly Clipper _clipper = new Clipper();

        private readonly IReadOnlyList<Vector2> _externalFootprint;
        public IReadOnlyList<Vector2> ExternalFootprint
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

        public FloorPlan(IReadOnlyList<Vector2> footprint)
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

        public IReadOnlyList<Vector2[]> TestRoom(IEnumerable<Vector2> roomFootprint, bool split = false, bool shrink = true)
        {
            //Generate shapes for this room footprint, early exit if null
            var solution = ShapesForRoom(roomFootprint, split);
            if (solution == null)
                return new Vector2[0][];

            //Convert shapes into vector2 shapes (scale properly)
            return solution
                .Select(shape => shape.Select(ToVector2)
                    .Shrink(shrink ? SAFE_DISTANCE : 0).ToArray()
                ).ToList();
        }

        private List<List<IntPoint>> ShapesForRoom(IEnumerable<Vector2> roomFootprint, bool split = false)
        {
            if (roomFootprint == null)
                throw new ArgumentNullException("roomFootprint");

            var clipperRoomFootprint = roomFootprint.Select(ToPoint).ToList();
            if (Clipper.Orientation(clipperRoomFootprint))
                throw new ArgumentException("Room footprint must be clockwise wound");

            //Contain within floor out edge
            var solution = ClipToFloor(clipperRoomFootprint, split);
            if (solution == null)
                return null;

            //Clip against other rooms
            if (_rooms.Count > 0)
            {
                var clips = solution.Select(a => ClipToRooms(a, split));

                solution = clips.SelectMany(a => a).ToList();
                if (solution.Count > 1 && !split)
                    return null;
            }

            //Ensure shapes are still clockwise wound (mutate in place to reverse)
            foreach (var shape in solution)
                if (Clipper.Orientation(shape))
                    shape.Reverse();
            
            return solution;
        }

        public IReadOnlyList<RoomPlan> AddRoom(IEnumerable<Vector2> roomFootprint, float wallThickness, IEnumerable<ScriptReference> scripts, bool split = false)
        {
            Contract.Requires<ArgumentNullException>(roomFootprint != null, "roomFootprint != null");
            Contract.Requires<ArgumentNullException>(scripts != null, "scripts != null");
            Contract.Ensures(Contract.Result<IReadOnlyList<RoomPlan>>() != null);

            if (_isFrozen)
                throw new InvalidOperationException("Cannot add rooms to floorplan once it is frozen");

            var solution = ShapesForRoom(roomFootprint, split);
            if (solution == null)
                return new RoomPlan[0];

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

        #region clipping
        private List<List<IntPoint>> ClipToRooms(List<IntPoint> roomFootprint, bool allowSplit)
        {
            _clipper.Clear();
            _clipper.AddPolygon(roomFootprint, PolyType.Subject);
            _clipper.AddPolygons(_rooms.Select(r => r.OuterFootprint.Select(ToPoint).ToList()).ToList(), PolyType.Clip);

            PolyTree solution = new PolyTree();
            _clipper.Execute(ClipType.Difference, solution);

            //Rooms with holes are not supported
            if (HasHole(solution))
            {
                //Rooms with holes are not supported (issue #166 - Will Not Fix)
                return new List<List<IntPoint>>();
            }

            var shapes = ToShapes(solution);

            if (shapes.Count > 1 && !allowSplit)
                return new List<List<IntPoint>>();

            return shapes;
        }

        private static bool HasHole(PolyNode tree)
        {
            if (tree.Contour.Count > 0 && tree.IsHole)
                return true;

            return tree.Childs.Any(HasHole);
        }

        private static List<List<IntPoint>> ToShapes(PolyTree tree)
        {
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            Clipper.PolyTreeToPolygons(tree, solution);
            return solution;
        }

        private List<List<IntPoint>> ClipToFloor(List<IntPoint> roomFootprint, bool allowSplit)
        {
            _clipper.Clear();
            _clipper.AddPolygon(roomFootprint, PolyType.Subject);
            _clipper.AddPolygon(_externalFootprint.Select(ToPoint).ToList(), PolyType.Clip);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            _clipper.Execute(ClipType.Intersection, solution);

            if (solution.Count > 1 && !allowSplit)
                return null;
            if (solution.Count == 0)
                return null;

            return solution;
        }
        #endregion

        public IEnumerable<Neighbour> GetNeighbours(RoomPlan room)
        {
            Contract.Requires<ArgumentNullException>(room != null, "room != null");
            Contract.Ensures(Contract.Result<IEnumerable<Neighbour>>() != null);

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

            public LineSegment2 Segment(RoomPlan room)
            {
                if (RoomAB == room)
                    return new LineSegment2(A, B);
                else
                    return new LineSegment2(C, D);
            }
        }
    }
}
