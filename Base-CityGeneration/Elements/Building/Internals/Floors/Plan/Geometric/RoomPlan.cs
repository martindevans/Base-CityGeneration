using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using EpimetheusPlugins.Procedural.Utilities;
using SwizzleMyVectors.Geometry;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric
{
    public class RoomPlan
        : IRoomPlan
    {
        #region fields and properties
        private readonly GeometricFloorplan _plan;

        private readonly IReadOnlyList<Vector2> _innerFootprint; 
        public IReadOnlyList<Vector2> InnerFootprint { get { return _innerFootprint; } }

        private readonly IReadOnlyList<Vector2> _outerFootprint;
        public IReadOnlyList<Vector2> OuterFootprint { get { return _outerFootprint; } }

        private readonly float _wallThickness;
        public float WallThickness { get { return _wallThickness; } }
        #endregion

        #region constructor
        internal RoomPlan(GeometricFloorplan plan, IReadOnlyList<Vector2> footprint, float wallThickness)
        {
            Contract.Requires(plan != null);
            Contract.Requires(footprint != null);

            _plan = plan;
            _innerFootprint = footprint.Shrink(wallThickness).ToArray();

            //Sometimes shrinking creates a different length array, if so then *unsrink* the shrunk array to create a new outer array (with the same length as the inner one)
            var outerFootprint = footprint.ToArray();
            if (InnerFootprint.Count != outerFootprint.Length)
                outerFootprint = InnerFootprint.Shrink(-wallThickness).ToArray();
            Walls.MatchUp(outerFootprint, InnerFootprint);
            _outerFootprint = outerFootprint;

            _wallThickness = wallThickness;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_plan != null);
            Contract.Invariant(_innerFootprint != null);
            Contract.Invariant(_outerFootprint != null);
        }
        #endregion

        public IEnumerable<Neighbour> Neighbours
        {
            get { return _plan.GetNeighbours(this); }
        }

        /// <summary>
        /// Get all facades surrounding this room. Facades either reach from the inner wall of this room, to the inner wall of a neighbouring room or they simple cover the width from inner->outer wall of this room (if there is no neighbour)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Facade> GetWalls()
        {
            Contract.Ensures(Contract.Result<IEnumerable<Facade>>() != null);

            var result = new List<Facade>();

            var roomNeighbours = _plan.GetNeighbours(this).ToArray();

            var sections = OuterFootprint.Sections(InnerFootprint).ToArray();
            for (var s = 0; s < sections.Length; s++)
            {
                var previousSection = sections[(s + sections.Length - 1) % sections.Length];
                var section = sections[s];
                var nextSection = sections[(s + 1) % sections.Length];

                Neighbour[] sectionNeighbours;

                if (IsNeighbourSection(section, roomNeighbours, out sectionNeighbours))
                {
                    result.AddRange(CreateNeighbourFacades(previousSection, nextSection, section, sectionNeighbours));
                }
                else if (IsExternalSection(section))
                {
                    result.Add(new Facade(null, true, section));
                }
                else
                {
                    result.Add(new Facade(null, false, section));
                }
            }

            for (var i = 0; i < result.Count; i++)
            {
                result[i].Next = result[(i + 1) % result.Count];
                result[i].Previous = result[(i + result.Count - 1) % result.Count];
            }

            return result;
        }

        private IEnumerable<Facade> CreateNeighbourFacades(Walls.Section previousSection, Walls.Section nextSection, Walls.Section section, Neighbour[] sectionNeighbours)
        {
            Contract.Requires(sectionNeighbours != null);
            Contract.Ensures(Contract.Result<IEnumerable<Facade>>() != null);

            var result = new List<Facade>();

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
            Array.Sort<Neighbour>(sectionNeighbours, CompareNeighboursAlongCommonEdge);

            float previousMax = 0;
            for (var i = 0; i < sectionNeighbours.Length; i++)
            {
                var neighbour = sectionNeighbours[i];

                var otherRoom = neighbour.RoomCD;
                var otherRoomInnerA = otherRoom.InnerFootprint[(int)neighbour.EdgeIndexRoomCD];
                var otherRoomInnerB = otherRoom.InnerFootprint[((int)neighbour.EdgeIndexRoomCD + 1) % otherRoom.OuterFootprint.Count];

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
                    var intersection = new LineSegment2(otherRoomInnerA, otherRoomInnerB).Line.Intersects(new Ray2(a, neighbour.D - neighbour.A));
                    if (!intersection.HasValue)
                        throw new InvalidOperationException("lines are parallel, this should never happen. (famous last words)");
                    adjustedD = intersection.Value.Position;
                }

                var adjustedC = neighbour.C;
                if (neighbour.Bt < startT || neighbour.Bt > (1 - endT))
                {
                    var intersection = new LineSegment2(otherRoomInnerA, otherRoomInnerB).Line.Intersects(new Ray2(b, neighbour.C - neighbour.B));
                    if (!intersection.HasValue)
                        throw new InvalidOperationException("lines are parallel, this should never happen. (famous last words)");
                    adjustedC = intersection.Value.Position;
                }

                if (Vector2.DistanceSquared(adjustedC, adjustedD) < 0.0001f)
                    continue;

                // 3. Clamp points on other room
                var otherRoomInnerAB = otherRoomInnerB - otherRoomInnerA;
                var clampedAdjustedCT = MathHelper.Clamp(new Ray2(otherRoomInnerA, otherRoomInnerAB).ClosestPointDistanceAlongLine(adjustedC), 0, 1);
                var clampedAdjustedDT = MathHelper.Clamp(new Ray2(otherRoomInnerA, otherRoomInnerAB).ClosestPointDistanceAlongLine(adjustedD), 0, 1);

                var c = otherRoomInnerA + otherRoomInnerAB * clampedAdjustedCT;
                var d = otherRoomInnerA + otherRoomInnerAB * clampedAdjustedDT;

                // 4. Projected clamped/projected points back to this room
                var intersectionABDA = new LineSegment2(section.B, section.A).Line.Intersects(new Ray2(d, neighbour.D - neighbour.A));
                if (!intersectionABDA.HasValue)
                    throw new InvalidOperationException("lines are parallel, this should never happen. (famous last words)");
                var reprojectedA = intersectionABDA.Value.Position;
                var reprojectedAT = intersectionABDA.Value.DistanceAlongA / section.Width;

                var intersectionABCB = new LineSegment2(section.B, section.A).Line.Intersects(new Ray2(c, neighbour.C - neighbour.B));
                if (!intersectionABCB.HasValue)
                    throw new InvalidOperationException("lines are parallel, this should never happen. (famous last words)");
                var reprojectedB = intersectionABCB.Value.Position;
                var reprojectedBT = intersectionABCB.Value.DistanceAlongA / section.Width;

                //Create section from last neighbour to edge of this one
                if (Math.Abs(reprojectedAT - previousMax) * section.Width > NeighbourData.SAME_POINT_EPSILON)
                {
                    var sA = section.B - section.Along * section.Width * previousMax;
                    var sC = new LineSegment2(section.C, section.D).ClosestPoint(reprojectedA);
                    var sD = section.C - section.Along * section.Width * previousMax;

                    result.Add(new Facade(null, false, new Walls.Section(false, sA, reprojectedA, sC, sD)));
                }

                //Create section from this room to neighbour
                result.Add(new Facade(otherRoom, false, new Walls.Section(false, reprojectedA, reprojectedB, c, d)));

                //If this is the last neighbour section, insert a facade from end of neighbour to end of wall
                if (i == sectionNeighbours.Length - 1 && Math.Abs(1 - reprojectedBT) * section.Width > NeighbourData.SAME_POINT_EPSILON)
                {
                    var eB = section.B - section.Along * section.Width * 1;
                    var eC = section.C - section.Along * section.Width * 1;
                    var eD = new LineSegment2(section.C, section.D).ClosestPoint(reprojectedB);

                    //Since this is the last section, create a section to the end
                    result.Add(new Facade(null, false, new Walls.Section(false, reprojectedB, eB, eC, eD)));
                }

                previousMax = reprojectedBT;
            }

            //Check if all sections were invalid, and if so just create a facade covering the entire section
            if (result.Count == 0)
                result.Add(new Facade(null, false, section));

            return result;
        }

        private int CompareNeighboursAlongCommonEdge(Neighbour a, Neighbour b)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);

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

                var innerEdge = new Ray2(section.C, section.D - section.C);

                Parallelism parallelism;
                innerEdge.Intersects(new Ray2(segmentSelf.Start, segmentSelf.End - segmentSelf.Start), out parallelism);

                return parallelism == Parallelism.Collinear;
            }).Where(n =>
            {
                var d1 = section.ExternalLineSegment.LongLine.ClosestPointDistanceAlongLine(n.A);
                var d2 = section.ExternalLineSegment.LongLine.ClosestPointDistanceAlongLine(n.B);

                return Math.Min(d1, d2) <= 1 && Math.Max(d1, d2) >= 0;
            }).ToArray();

            return neighbourSection.Length != 0;
        }

        private bool IsExternalSection(Walls.Section section)
        {
            if (section.IsCorner)
            {
                //Corner sections have 2 edges which may be external, A->B and B->C
                return
                    IsExternalLineSegment(new LineSegment2(section.A, section.B)) ||
                    IsExternalLineSegment(new LineSegment2(section.B, section.C));
            }
            else
            {
                return IsExternalLineSegment(section.ExternalLineSegment);
            }
        }

        private bool IsExternalLineSegment(LineSegment2 segment)
        {
            var externalLines = IRoomPlanExtensions.Edges(_plan.ExternalFootprint.ToArray()).Select(edge => new LineSegment2(edge.Start, edge.End));

            return externalLines.Any(l =>
            {
                var edgeDirection = l.Line.Direction;
                var segmentDirection = segment.Line.Direction;

                if (Math.Abs(Vector2.Dot(edgeDirection, segmentDirection)) < 0.99619469809f) //Allow 5 degrees difference
                    return false;

                return
                    l.DistanceToPoint(segment.Start) < (WallThickness * 2) &&
                    l.DistanceToPoint(segment.End) < (WallThickness * 2);
            });
        }

        
    }
}
