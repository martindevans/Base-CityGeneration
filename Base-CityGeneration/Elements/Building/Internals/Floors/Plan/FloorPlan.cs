using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class FloorPlan
    {
        #region fields/properties
        private const float SCALE = 100000;

        private bool _isFrozen = false;
        private readonly Clipper _clipper = new Clipper();

        private readonly Vector2[] _footprint;
        public IEnumerable<Vector2> Footprint
        {
            get { return _footprint; }
        }

        private readonly float _externalWallThickness;

        private readonly List<RoomInfo> _rooms = new List<RoomInfo>();
        public IEnumerable<RoomInfo> Rooms
        {
            get { return _rooms; }
        }

        private readonly NeighbourData _neighbourhood;
        #endregion

        public FloorPlan(Vector2[] footprint, float externalWallThickness)
        {
            _footprint = footprint;
            _externalWallThickness = externalWallThickness;

            _neighbourhood = new NeighbourData(this);
        }

        public void Freeze()
        {
            _isFrozen = true;

            _neighbourhood.GenerateNeighbours();
        }

        private int _nextRoomId = 0;

        public IEnumerable<RoomInfo> AddRoom(IEnumerable<Vector2> roomFootprint, float wallThickness, IEnumerable<ScriptReference> scripts, bool split = false)
        {
            if (_isFrozen)
                throw new InvalidOperationException("Cannot add rooms to floorplan once it is frozen");

            var clipperRoomFootprint = roomFootprint.Select(ToPoint).ToList();

            if (Clipper.Orientation(clipperRoomFootprint))
                throw new ArgumentException("Room footprint must be clockwise wound");

            //Contain room inside floor
            _clipper.Clear();
            _clipper.AddPolygon(clipperRoomFootprint, PolyType.Subject);
            _clipper.AddPolygon(_footprint.Select(ToPoint).ToList(), PolyType.Clip);
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            _clipper.Execute(ClipType.Intersection, solution);

            if (solution.Count > 1 && !split)
                return new RoomInfo[0];

            //Clip against other rooms
            if (_rooms.Count > 0)
            {
                _clipper.Clear();
                _clipper.AddPolygons(solution, PolyType.Subject);
                _clipper.AddPolygons(_rooms.Select(r => r.OuterFootprint.Select(ToPoint).ToList()).ToList(), PolyType.Clip);
                solution.Clear();
                _clipper.Execute(ClipType.Difference, solution, PolyFillType.NonZero, PolyFillType.NonZero);

                if (solution.Count > 1 && !split)
                    return new RoomInfo[0];
            }

            var s = scripts.ToArray();

            List<RoomInfo> result = new List<RoomInfo>();
            foreach (var shape in solution)
            {
                _neighbourhood.Dirty = true;

                //Ensure shape is still clockwise wound
                if (Clipper.Orientation(shape))
                    shape.Reverse();

                var r = new RoomInfo(this, shape.Select(ToVector2).ToArray(), wallThickness, s, _nextRoomId++);
                result.Add(r);
                _rooms.Add(r);
            }

            return result;
        }

        public IEnumerable<Neighbour> GetNeighbours(RoomInfo room)
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

        public class RoomInfo
        {
            private readonly FloorPlan _plan;

            public readonly Vector2[] InnerFootprint;
            public readonly Vector2[] OuterFootprint;

            public readonly ScriptReference[] Scripts;
            public readonly float WallThickness;

            public readonly int Id;

            public IRoom Node;

            internal RoomInfo(FloorPlan plan, Vector2[] footprint, float wallThickness, ScriptReference[] scripts, int id)
            {
                _plan = plan;
                OuterFootprint = footprint;
                InnerFootprint = footprint.Shrink(wallThickness).ToArray();
                Walls.MatchUp(OuterFootprint, InnerFootprint);

                Scripts = scripts;
                WallThickness = wallThickness;
                Id = id;
            }

            /// <summary>
            /// Get all facades surrounding this room. Facades either reach from the inner wall of this room, to the inner wall of a neighbouring room or they simple cover the width from inner->outer wall of this room (if there is no neighbour)
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Facade> GetFacades()
            {
                var result = new HashSet<Facade>();

                var roomNeighbours = _plan.GetNeighbours(this).ToArray();

                foreach (var section in OuterFootprint.Sections(InnerFootprint).ToArray())
                {
                    Neighbour[] sectionNeighbours;

                    if (IsExternalSection(section))
                    {
                        result.Add(new Facade(null, true, section));
                    }
                    else if (IsNeighbourSection(section, roomNeighbours, out sectionNeighbours))
                    {
                        Array.Sort<Neighbour>(sectionNeighbours, CompareNeighboursAlongCommonEdge);

                        float previousMax = 0;
                        for (int i = 0; i < sectionNeighbours.Length; i++)
                        {
                            var neighbour = sectionNeighbours[i];

                            float min = Math.Min(neighbour.At, neighbour.At);
                            float max = Math.Max(neighbour.At, neighbour.At);

                            //Create section from last neighbour to edge of this one
                            var sA = section.A + section.Along * section.Width * previousMax;
                            var sB = section.A + section.Along * section.Width * min;
                            var sC = section.D + section.Along * section.Width * min;
                            var sD = section.D + section.Along * section.Width * previousMax;
                            result.Add(new Facade(null, false, new Walls.Section(false, sA, sB, sC, sD)));

                            //Create section from this room to neighbour
                            result.Add(new Facade(neighbour.Other(this), false, new Walls.Section(false, neighbour.A, neighbour.B, neighbour.C, neighbour.D)));

                            if (i == sectionNeighbours.Length - 1)
                            {
                                //Since this is the last section, create a section to the end
                                var eA = section.A + section.Along * section.Width * max;
                                var eB = section.A + section.Along * section.Width * 1;
                                var eC = section.D + section.Along * section.Width * 1;
                                var eD = section.D + section.Along * section.Width * max;
                                result.Add(new Facade(null, false, new Walls.Section(false, eA, eB, eC, eD)));
                            }

                            previousMax = max;
                        }
                    }
                    else
                    {
                        result.Add(new Facade(null, false, section));
                    }
                }

                return result;
            }

            private int CompareNeighboursAlongCommonEdge(Neighbour a, Neighbour b)
            {
                if (a.RoomAB != this)
                    throw new ArgumentException("room adjacent to neighbour data is not this room", "a");
                if (b.RoomAB != this)
                    throw new ArgumentException("room adjacent to neighbour data is not this room", "a");

                //Create a comparator function to compare neighbours along a common edge of this room
                //Find out which points lie along the egde of this room, and then compare then

                if (Math.Max(a.At, a.Bt) <= Math.Min(b.At, b.Bt))
                    return -1;
                else if (Math.Min(a.At, a.Bt) >= Math.Max(b.At, a.Bt))
                    return 1;
                else
                    return 0;
            }

            private bool IsNeighbourSection(Walls.Section section, IEnumerable<Neighbour> neighbours, out Neighbour[] neighbourSection)
            {
                if (section.IsCorner)
                {
                    neighbourSection = null;
                    return false;
                }

                neighbourSection = neighbours.Where(n =>
                {
                    var segmentSelf = n.Segment(this);

                    var innerEdge = new Line2D(section.A, section.B - section.A);

                    Geometry2D.Parallelism parallelism;
                    Geometry2D.LineLineIntersection(innerEdge, new Line2D(segmentSelf.Start, segmentSelf.End - segmentSelf.Start), out parallelism);

                    return parallelism == Geometry2D.Parallelism.Collinear;
                }).ToArray();

                return neighbourSection.Length != 0;
            }

            private bool IsExternalSection(Walls.Section section)
            {
                foreach (var outerEdge in Edges(_plan.Footprint.ToArray()).Select(edge => new Line2D(edge.Start, edge.End - edge.Start)))
                {
                    Geometry2D.Parallelism parallelism;
                    if (section.IsCorner)
                    {
                        //Corner sections have 2 edges which may be external, A->B and B->C
                        Geometry2D.LineLineIntersection(new Line2D(section.A, section.B - section.A), outerEdge, out parallelism);
                        if (parallelism != Geometry2D.Parallelism.Collinear)
                            Geometry2D.LineLineIntersection(new Line2D(section.B, section.C - section.B), outerEdge, out parallelism);
                    }
                    else
                        Geometry2D.LineLineIntersection(new Line2D(section.C, section.Along), outerEdge, out parallelism);

                    if (parallelism == Geometry2D.Parallelism.Collinear)
                        return true;
                }

                return false;
            }

            private static IEnumerable<LineSegment2D> Edges(IList<Vector2> array)
            {
                for (int i = 0; i < array.Count; i++)
                    yield return new LineSegment2D(array[i], array[(i + 1) % array.Count]);
            }

            public struct Facade
            {
                private readonly RoomInfo _neighbouringRoom;
                public RoomInfo NeighbouringRoom { get { return _neighbouringRoom; } }

                private readonly bool _isExternal;
                public bool IsExternal { get { return _isExternal; } }

                private readonly Walls.Section _section;
                public Walls.Section Section { get { return _section; } }

                    public Facade(RoomInfo other, bool external, Walls.Section section)
                {
                    _neighbouringRoom = other;
                    _isExternal = external;
                    _section = section;
                }
            }
        }

        public class Neighbour
        {
            /// <summary>
            /// The index of the edge on roomAB
            /// </summary>
            public uint EdgeIndexRoomAB { get; private set; }

            /// <summary>
            /// One of the rooms in a neighbourship pair (touching points A and B)
            /// </summary>
            public RoomInfo RoomAB { get; private set; }

            /// <summary>
            /// The index of the edge on roomCD
            /// </summary>
            public uint EdgeIndexRoomCD { get; private set; }

            /// <summary>
            /// One of the rooms in a neighbourship pair (touching points C and D)
            /// </summary>
            public RoomInfo RoomCD { get; private set; }

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

            public Neighbour(uint edgeAbIndex, RoomInfo ab, uint edgeCdIndex, RoomInfo cd, Vector2 a, float at, Vector2 b, float bt, Vector2 c, float ct, Vector2 d, float dt)
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

            public RoomInfo Other(RoomInfo room)
            {
                if (RoomAB == room)
                    return RoomCD;
                else
                    return RoomAB;
            }

            public LineSegment2D Segment(RoomInfo room)
            {
                if (RoomAB == room)
                    return new LineSegment2D(A, B);
                else
                    return new LineSegment2D(C, D);
            }
        }
    }
}
