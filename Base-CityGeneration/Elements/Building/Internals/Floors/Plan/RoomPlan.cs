using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    public class RoomPlan
    {
        private readonly FloorPlan _plan;

        public readonly Vector2[] InnerFootprint;
        public readonly Vector2[] OuterFootprint;

        public readonly ScriptReference[] Scripts;
        public readonly float WallThickness;

        public readonly int Id;

        public IPlannedRoom Node;

        internal RoomPlan(FloorPlan plan, Vector2[] footprint, float wallThickness, ScriptReference[] scripts, int id)
        {
            _plan = plan;
            OuterFootprint = footprint;
            InnerFootprint = footprint.Shrink(wallThickness).ToArray();

            //Sometimes shrinking creates a different length array, if so then *unsrink* the shrunk array to create a new outer array (with the same length as the inner one)
            if (InnerFootprint.Length != OuterFootprint.Length)
                OuterFootprint = InnerFootprint.Shrink(-wallThickness).ToArray();

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

            var sections = OuterFootprint.Sections(InnerFootprint).ToArray();
            for (int s = 0; s < sections.Length; s++)
            {
                var previousSection = sections[(s + sections.Length - 1) % sections.Length];
                var section = sections[s];
                var nextSection = sections[(s + 1) % sections.Length];

                FloorPlan.Neighbour[] sectionNeighbours;

                if (IsExternalSection(section))
                {
                    result.Add(new Facade(null, true, section));
                }
                else if (IsNeighbourSection(section, roomNeighbours, out sectionNeighbours))
                {
                    result.UnionWith(CreateNeighbourFacades(previousSection, nextSection, section, sectionNeighbours));
                }
                else
                {
                    result.Add(new Facade(null, false, section));
                }
            }

            return result;
        }

        private IEnumerable<Facade> CreateNeighbourFacades(Walls.Section previousSection, Walls.Section nextSection, Walls.Section section, FloorPlan.Neighbour[] sectionNeighbours)
        {
            List<Facade> result = new List<Facade>();

            //Length of the wall consumed in the corner sections
            var startCornerLength = Vector2.Distance(previousSection.B, previousSection.C);
            var endCornerLength = Vector2.Distance(nextSection.A, nextSection.B);

            //Total length of edge
            var totalLength = startCornerLength + section.Width + endCornerLength;

            //Precentage of total length taken up in each part
            var startT = startCornerLength / totalLength;
            var middleT = section.Width / totalLength;
            var endT = endCornerLength / totalLength;

            //Move through the neighbour sections in order
            Array.Sort<FloorPlan.Neighbour>(sectionNeighbours, CompareNeighboursAlongCommonEdge);

            float previousMax = 0;
            for (int i = 0; i < sectionNeighbours.Length; i++)
            {
                var neighbour = sectionNeighbours[i];

                var otherRoom = neighbour.RoomCD;
                var otherRoomInnerA = otherRoom.InnerFootprint[neighbour.EdgeIndexRoomCD];
                var otherRoomInnerB = otherRoom.InnerFootprint[(neighbour.EdgeIndexRoomCD + 1) % otherRoom.OuterFootprint.Length];

                // 1. Clamp points to lie in valid range on this room
                var aT = (MathHelper.Clamp(neighbour.At, startT, 1 - endT) - startT) / middleT;
                var bT = (MathHelper.Clamp(neighbour.Bt, startT, 1 - endT) - startT) / middleT;

                if (Math.Abs(aT - bT) < float.Epsilon)
                    continue;

                var a = section.B - section.Along * section.Width * aT;
                var b = section.B - section.Along * section.Width * bT;

                // 2. Project clamped points back onto other room
                var adjustedD = neighbour.D;
                if (neighbour.At < startT || neighbour.At > (1 - endT))
                {
                    var intersection = Geometry2D.LineLineIntersection(new LineSegment2D(otherRoomInnerA, otherRoomInnerB).Line(), new Line2D(a, neighbour.D - neighbour.A));
                    if (!intersection.HasValue)
                        throw new NotImplementedException();
                    adjustedD = intersection.Value.Position;
                }

                var adjustedC = neighbour.C;
                if (neighbour.Bt < startT || neighbour.Bt > (1 - endT))
                {
                    var intersection = Geometry2D.LineLineIntersection(new LineSegment2D(otherRoomInnerA, otherRoomInnerB).Line(), new Line2D(b, neighbour.C - neighbour.B));
                    if (!intersection.HasValue)
                        throw new NotImplementedException();
                    adjustedC = intersection.Value.Position;
                }

                if (Vector2.DistanceSquared(adjustedC, adjustedD) < 0.0001f)
                    continue;

                // 3. Clamp points on other room
                var otherRoomInnerAB = otherRoomInnerB - otherRoomInnerA;
                var adjustedCT = Geometry2D.ClosestPointDistanceAlongLine(new Line2D(otherRoomInnerA, otherRoomInnerAB), adjustedC);
                var adjustedDT = Geometry2D.ClosestPointDistanceAlongLine(new Line2D(otherRoomInnerA, otherRoomInnerAB), adjustedD);

                var clampedAdjustedCT = MathHelper.Clamp(adjustedCT, 0, 1);
                var clampedAdjustedDT = MathHelper.Clamp(adjustedDT, 0, 1);

                var c = otherRoomInnerA + otherRoomInnerAB * clampedAdjustedCT;
                var d = otherRoomInnerA + otherRoomInnerAB * clampedAdjustedDT;

                // 4. Projected clamped/projected points back to this room
                var intersectionABDA = Geometry2D.LineLineIntersection(new LineSegment2D(section.A, section.B).Line(), new Line2D(d, neighbour.D - neighbour.A));
                if (!intersectionABDA.HasValue)
                    throw new NotImplementedException();
                var reprojectedA = intersectionABDA.Value.Position;

                var intersectionABCB = Geometry2D.LineLineIntersection(new LineSegment2D(section.A, section.B).Line(), new Line2D(c, neighbour.C - neighbour.B));
                if (!intersectionABCB.HasValue)
                    throw new NotImplementedException();
                var reprojectedB = intersectionABCB.Value.Position;

                //Create section from this room to neighbour
                result.Add(new Facade(otherRoom, false, new Walls.Section(false, reprojectedA, reprojectedB, c, d)));

                //Create section from last neighbour to edge of this one
                if (Math.Abs(aT - previousMax) > float.Epsilon)
                {
                    var sA = section.B - section.Along * section.Width * previousMax;
                    var sC = Geometry2D.ClosestPointOnLineSegment(new LineSegment2D(section.C, section.D), reprojectedA);
                    var sD = section.C - section.Along * section.Width * previousMax;

                    result.Add(new Facade(null, false, new Walls.Section(false, sA, reprojectedA, sC, sD)));
                }

                if (i == sectionNeighbours.Length - 1 && Math.Abs(neighbour.Bt - 1) > float.Epsilon)
                {
                    var eB = section.B - section.Along * section.Width * 1;
                    var eC = section.C - section.Along * section.Width * 1;
                    var eD = Geometry2D.ClosestPointOnLineSegment(new LineSegment2D(section.C, section.D), reprojectedB);

                    //Since this is the last section, create a section to the end
                    result.Add(new Facade(null, false, new Walls.Section(false, reprojectedB, eB, eC, eD)));
                }

                previousMax = Geometry2D.ClosestPointDistanceAlongLine(new Line2D(section.C, section.D - section.C), reprojectedB);
            }

            //Check if all sections were invalid, and if so just create a facade covering the entire section
            if (result.Count == 0)
                result.Add(new Facade(null, false, section));

            return result;
        }

        private int CompareNeighboursAlongCommonEdge(FloorPlan.Neighbour a, FloorPlan.Neighbour b)
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

        private bool IsNeighbourSection(Walls.Section section, IEnumerable<FloorPlan.Neighbour> neighbours, out FloorPlan.Neighbour[] neighbourSection)
        {
            if (section.IsCorner)
            {
                neighbourSection = null;
                return false;
            }

            neighbourSection = neighbours.Where(n =>
            {
                var segmentSelf = n.Segment(this);

                var innerEdge = new Line2D(section.C, section.D - section.C);

                Geometry2D.Parallelism parallelism;
                Geometry2D.LineLineIntersection(innerEdge, new Line2D(segmentSelf.Start, segmentSelf.End - segmentSelf.Start), out parallelism);

                return parallelism == Geometry2D.Parallelism.Collinear;
            }).ToArray();

            return neighbourSection.Length != 0;
        }

        private bool IsExternalSection(Walls.Section section)
        {
            if (section.IsCorner)
            {
                //Corner sections have 2 edges which may be external, A->B and B->C
                return
                    IsExternalLineSegment(new LineSegment2D(section.A, section.B)) ||
                    IsExternalLineSegment(new LineSegment2D(section.B, section.C));
            }
            else
            {
                return IsExternalLineSegment(section.ExternalLineSegment);
            }
        }

        private bool IsExternalLineSegment(LineSegment2D segment)
        {
            var externalLines = Edges(_plan.ExternalFootprint.ToArray()).Select(edge => new LineSegment2D(edge.Start, edge.End));

            return externalLines.Any(l =>
            {
                Geometry2D.Parallelism parallelism;
                Geometry2D.LineLineIntersection(l.Line(), segment.Line(), out parallelism);
                if (parallelism == Geometry2D.Parallelism.Collinear)
                    return true;
                else if (parallelism == Geometry2D.Parallelism.Parallel)
                {
                    return
                        Geometry2D.DistanceFromPointToLineSegment(segment.Start, l) < 0.05f &&
                        Geometry2D.DistanceFromPointToLineSegment(segment.End, l) < 0.05f;
                }
                else
                    return false;
            });
        }

        private static IEnumerable<LineSegment2D> Edges(IList<Vector2> array)
        {
            return array
                .Select((t, i) => new LineSegment2D(t, array[(i + 1) % array.Count]));
        }

        public struct Facade
        {
            private readonly RoomPlan _neighbouringRoom;
            public RoomPlan NeighbouringRoom { get { return _neighbouringRoom; } }

            private readonly bool _isExternal;
            public bool IsExternal { get { return _isExternal; } }

            private readonly Walls.Section _section;
            public Walls.Section Section { get { return _section; } }

            public Facade(RoomPlan other, bool external, Walls.Section section)
            {
                _neighbouringRoom = other;
                _isExternal = external;
                _section = section;
            }
        }
    }
}
