using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric
{
    internal class NeighbourData
    {
        #region fields/properties
        internal const float SAME_POINT_EPSILON = 0.1f;
        internal const float SAME_POINT_EPSILON_SQR = SAME_POINT_EPSILON * SAME_POINT_EPSILON;

        private readonly IFloorPlanBuilder _plan;

        public bool Dirty { get; set; }

        private Dictionary<IRoomPlan, List<Neighbour>> _neighbours;

        public IEnumerable<Neighbour> this[IRoomPlan key]
        {
            get
            {
                Contract.Requires(key != null);
                Contract.Ensures(Contract.Result<IEnumerable<Neighbour>>() != null);

                List<Neighbour> value;
                if (!_neighbours.TryGetValue(key, out value) || value == null)
                    throw new InvalidOperationException("Failed to find neighbour information for given room");

                return value;
            }
        }
        #endregion

        public NeighbourData(IFloorPlanBuilder plan)
        {
            Contract.Requires(plan != null);

            _plan = plan;

            Dirty = true;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_plan != null);
        }

        public void GenerateNeighbours()
        {
            if (!Dirty)
                return;

            _neighbours = _plan.Rooms.ToDictionary(a => a, a => new List<Neighbour>());

            foreach (var room in _plan.Rooms)
            {
                var l = _neighbours[room];

                //Map points onto this edge
                foreach (var edge in Edges(room))
                {
                    var edgeLine = new Ray2(edge.Segment.Start, edge.Segment.End - edge.Segment.Start);

                    foreach (var otherRoom in _plan.Rooms)
                    {
                        if (ReferenceEquals(room, otherRoom))
                            continue;

                        ProjectPointsOntoEdge(otherRoom, edgeLine, edge);
                    }

                    l.AddRange(ExtractNeighbourSections(room, edge));
                }
            }

            Dirty = false;
        }

        private static IEnumerable<Neighbour> ExtractNeighbourSections(IRoomPlan room, Edge edge)
        {
            if (edge.EdgeList.Count == 0)
                return new Neighbour[0];

            //Sort by distance along this edge
            //Ties are resolved by putting the closer point first
            edge.EdgeList.Sort((a, b) =>
            {
                var ret = a.Pt.CompareTo(b.Pt);
                if (ret == 0)
                    ret = a.Distance.CompareTo(b.Distance);
                return ret;
            });

            //Now we have a load of markers along the edge of this room which mark where the edge of other rooms project onto this edge
            //Walk along list pairing them up

            List<Neighbour> neighbours = new List<Neighbour>();

            for (int i = 0; i < edge.EdgeList.Count; i++)
            {
                var a = edge.EdgeList[i];
                var b = a.NaturalPair;

                //Skip this pair, we'll handle it when we come across it the other way around
                if (b.Pt <= a.Pt)
                    continue;

                //Get points which are the start on an overlapping segment
                var overlaps = edge.EdgeList.Where(x => SegmentOverlap(a, b, x)).ToArray();

                //Create segment
                var segmentNeighbours = new List<Neighbour>();
                AddNeighbour(segmentNeighbours, edge.Index, room, a, b);

                //Narrow down segment by slicing out parts where an occluding overlap occurs
                foreach (var o1 in overlaps)
                {
                    var o2 = o1.NaturalPair;
                    Contract.Assert(o2 != null);

                    for (var k = 0; k < segmentNeighbours.Count; k++)
                    {
                        var s = segmentNeighbours[k];

                        // Possible cases:
                        // 1. Overlap totally contains segment, remove segment
                        // 2. Overlap start is within segment, remove segment and replace with start -> middle
                        // 3. Overlap end is within segment, remove segment and replace with middle -> end
                        // 4. Overlap is totally within segment, remove segment and replace with start -> mid1 and mid2 -> end
                        // 5. There is no overlap at all, this should not be possible

                        if (o1.Pt <= s.At && o2.Pt >= s.Bt)
                        {
                            //Total overlap (Case 1)
                            segmentNeighbours.RemoveAt(k);
                        }
                        else if (o1.Pt >= s.At && o1.Pt <= s.Bt && o2.Pt >= s.At && o2.Pt <= s.Bt)  //Overlap with segment (Case 4)
                        {
                            //Remove segment
                            var removed = segmentNeighbours[k];
                            segmentNeighbours.RemoveAt(k);

                            //Replace with start -> mid1
                            AddNeighbour_N_To_Info(segmentNeighbours, edge.Index, room, removed, true, o1);

                            //Replace with mid2 -> end
                            AddNeighbour_N_To_Info(segmentNeighbours, edge.Index, room, removed, false, o2);
                        }
                        else if (o1.Pt >= s.At && o1.Pt <= s.Bt)        //Overlap start within segment (Case 2)
                        {
                            //Remove segment
                            var removed = segmentNeighbours[k];
                            segmentNeighbours.RemoveAt(k);

                            //Replace with start -> mid
                            AddNeighbour_N_To_Info(segmentNeighbours, edge.Index, room, removed, true, o1);
                        }
                        else if (o2.Pt >= s.At && o2.Pt <= s.Bt)        //Overlap end within segment (Case 3)
                        {
                            //Remove segment
                            var removed = segmentNeighbours[k];
                            segmentNeighbours.RemoveAt(k);

                            //Replace with mid -> end
                            AddNeighbour_N_To_Info(segmentNeighbours, edge.Index, room, removed, false, o2);
                        }
                        else
                            throw new InvalidOperationException("No overlap");
                    }
                }

                neighbours.AddRange(segmentNeighbours);
            }

            return neighbours;
        }

        private static bool SegmentOverlap(NeighbourInfo a, NeighbourInfo b, NeighbourInfo potentialOverlapPoint)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Requires(potentialOverlapPoint != null);
            Contract.Requires(potentialOverlapPoint.NaturalPair != null);

            var x = potentialOverlapPoint;
            var y = potentialOverlapPoint.NaturalPair;

            if (y.Pt <= x.Pt)
                return false;
            if (potentialOverlapPoint == a || potentialOverlapPoint == b)
                return false;

            return (x.Distance < Math.Min(a.Distance, b.Distance) || y.Distance < Math.Min(a.Distance, b.Distance))
                && (SegmentContains(x, y, a) || SegmentContains(x, y, b) || SegmentContains(a, b, x) ||SegmentContains(a, b, y));
        }

        private static bool SegmentContains(NeighbourInfo a, NeighbourInfo b, NeighbourInfo point)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);
            Contract.Requires(point != null);

            return a.Pt <= point.Pt && b.Pt >= point.Pt;
        }

        private static void AddNeighbour_N_To_Info(ICollection<Neighbour> neighbours, uint edgeIndex, IRoomPlan room, Neighbour n, bool nA, NeighbourInfo info)
        {
            Contract.Requires(neighbours != null);
            Contract.Requires(room != null);
            Contract.Requires(n != null);
            Contract.Requires(info != null);

            var lineOut = new Ray2(info.Point, info.OtherPoint - info.Point);
            var otherEdge = GetEdge(n.RoomCD, n.EdgeIndexRoomCD).Segment;
            var otherEdgeLine = new Ray2(otherEdge.Start, otherEdge.End - otherEdge.Start);

            var proj = lineOut.Intersects(otherEdgeLine);
            if (!proj.HasValue)
                throw new InvalidOperationException("Reprojected segment section does not lie on other edge");

            AddNeighbour(neighbours, edgeIndex, room, nA ? ToNeighbourInfoAD(n) : ToNeighbourInfoBC(n), new NeighbourInfo
            {
                Distance = nA ? Vector2.Distance(n.A, n.D) : Vector2.Distance(n.B, n.C),
                NaturalPair = null,
                Point = info.Point,
                Pt = info.Pt,
                OtherPoint = proj.Value.Position,
                OPt = proj.Value.DistanceAlongB,
                OtherRoom = n.RoomCD,
                OtherEdgeIndex = n.EdgeIndexRoomCD
            });
        }

        private static NeighbourInfo ToNeighbourInfoAD(Neighbour n)
        {
            Contract.Requires(n != null);
            Contract.Ensures(Contract.Result<NeighbourInfo>() != null);

            return new NeighbourInfo
            {
                Distance = Vector2.Distance(n.A, n.D),
                NaturalPair = null,
                OtherEdgeIndex = n.EdgeIndexRoomCD,
                Point = n.A,
                Pt = n.At,
                OtherPoint = n.D,
                OPt = n.Dt,
                OtherRoom = n.RoomCD
            };
        }

        private static NeighbourInfo ToNeighbourInfoBC(Neighbour n)
        {
            Contract.Requires(n != null);
            Contract.Ensures(Contract.Result<NeighbourInfo>() != null);

            return new NeighbourInfo
            {
                Distance = Vector2.Distance(n.B, n.C),
                NaturalPair = null,
                OtherEdgeIndex = n.EdgeIndexRoomCD,
                Point = n.B,
                Pt = n.Bt,
                OtherPoint = n.C,
                OPt = n.Ct,
                OtherRoom = n.RoomCD
            };
        }

        private static void AddNeighbour(ICollection<Neighbour> list, uint edgeIndex, IRoomPlan room, NeighbourInfo a, NeighbourInfo b)
        {
            Contract.Requires(list != null);
            Contract.Requires(room != null);
            Contract.Requires(a != null);
            Contract.Requires(b != null);

            //Swap points if order is reversed
            if (a.Pt > b.Pt)
            {
                var t = a;
                a = b;
                b = t;
            }

            if (Vector2.Distance(a.Point, b.Point) < 0.05f)
                return;

            list.Add(new Neighbour(edgeIndex, room, a.OtherEdgeIndex, a.OtherRoom,
                a.Point, a.Pt,
                b.Point, b.Pt,
                b.OtherPoint, b.OPt,
                a.OtherPoint, a.OPt
            ));
        }

        private static void ProjectPointsOntoEdge(IRoomPlan otherRoom, Ray2 edgeLine, Edge edge)
        {
            Contract.Requires(otherRoom != null);

            foreach (var otherEdge in Edges(otherRoom))
            {
                var otherEdgeLine = new Ray2(otherEdge.Segment.Start, otherEdge.Segment.End - otherEdge.Segment.Start);

                if (Vector2.Dot(edgeLine.Direction.Perpendicular(), otherEdgeLine.Direction.Perpendicular()) >= 0)
                    continue;

                var lCount = 0;
                if (edgeLine.IsLeft(otherEdge.Segment.Start, float.Epsilon))
                    lCount++;
                if (edgeLine.IsLeft(otherEdge.Segment.End, float.Epsilon))
                    lCount++;
                if (otherEdgeLine.IsLeft(edge.Segment.Start, float.Epsilon))
                    lCount++;
                if (otherEdgeLine.IsLeft(edge.Segment.End, float.Epsilon))
                    lCount++;
                if (lCount < 3)
                    continue;

                //4 possibilities for how these edges overlap:
                //
                // No overlap:
                //      B------------A
                //                     C-------D
                //
                // End Overlap
                //      B------------A
                //               C-------D
                //
                // Contained:
                //      B------------A
                //         C-------D
                //
                // Start Overlap
                //      B------------A
                // C-------D
                //
                // Reverse Contained:
                //      B------------A
                // C---------------------D

                var c = otherEdge.Segment.Start;
                var ct = edgeLine.ClosestPointDistanceAlongLine(c);
                var d = otherEdge.Segment.End;
                var dt = edgeLine.ClosestPointDistanceAlongLine(d);

                //Check that the edge is the correct direction
                if (ct < dt)
                    throw new InvalidOperationException("Edge is wound incorrectly");

                // If c is before the line then logically d must also be off the line
                //      B------------A
                //                       C-------D
                //
                // If d is after the line, then logically c must be too
                //             B------------A
                // C-------D
                if (ct < 0 || dt > 1)
                    continue;

                if (ct <= 1)
                {
                    if (dt < 0)
                    {
                        // End Overlap
                        //      B------------A
                        //               C-------D
                        var at = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.Start);

                        CreateNeighbourInfoPair(otherRoom, edge, ct, 0, otherEdge, 0, at);
                    }
                    else //if (dt >= 0 && dt <= 1)// this is always true (has to do with the `ct < dt` check above, thanks to code contracts!)
                    {
                        // Contained:
                        //      B------------A
                        //         C-------D
                        CreateNeighbourInfoPair(otherRoom, edge, ct, 0, otherEdge, dt, 1);
                    }
                }
                else
                {
                    if (dt > 0)
                    {
                        // Start Overlap
                        //      B------------A
                        // C-------D
                        var bt = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.End);

                        CreateNeighbourInfoPair(otherRoom, edge, dt, 1, otherEdge, 1, bt);
                    }
                    else
                    {
                        // Reverse Contained:
                        //      B------------A
                        // C---------------------D
                        var at = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.Start);
                        var bt = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.End);

                        CreateNeighbourInfoPair(otherRoom, edge, 0, at, otherEdge, 1, bt);
                    }
                }
            }
        }

        private static void CreateNeighbourInfoPair(IRoomPlan otherRoom,
            Edge edge, float point1DistanceAlongEdge, float point1ProjDistanceAlongOtherEdge,
            Edge otherEdge, float point2DistanceAlongEdge, float point2ProjDistanceAlongOtherEdge)
        {
            Contract.Requires(otherRoom != null);
            Contract.Requires(edge.EdgeList != null);

            var edgeDirection = (edge.Segment.End - edge.Segment.Start);
            var point1OnEdge = edge.Segment.Start + edgeDirection * point1DistanceAlongEdge;
            var point2OnEdge = edge.Segment.Start + edgeDirection * point2DistanceAlongEdge;

            var otherEdgeDirection = (otherEdge.Segment.End - otherEdge.Segment.Start);
            var point1ProjectedOntoOtherEdge = otherEdge.Segment.Start + otherEdgeDirection * point1ProjDistanceAlongOtherEdge;
            var point2ProjectedOntoOtherEdge = otherEdge.Segment.Start + otherEdgeDirection * point2ProjDistanceAlongOtherEdge;

            var x = new NeighbourInfo
            {
                Distance = Vector2.Distance(point1OnEdge, point1ProjectedOntoOtherEdge),
                Point = point1OnEdge,
                OtherEdgeIndex = otherEdge.Index,
                OtherPoint = point1ProjectedOntoOtherEdge,
                OtherRoom = otherRoom,
                Pt = point1DistanceAlongEdge,
                OPt = point1ProjDistanceAlongOtherEdge
            };

            var y = new NeighbourInfo
            {
                Distance = Vector2.Distance(point2OnEdge, point2ProjectedOntoOtherEdge),
                Point = point2OnEdge,
                OtherEdgeIndex = otherEdge.Index,
                OtherPoint = point2ProjectedOntoOtherEdge,
                OtherRoom = otherRoom,
                Pt = point2DistanceAlongEdge,
                OPt = point2ProjDistanceAlongOtherEdge
            };

            x.NaturalPair = y;
            y.NaturalPair = x;

            edge.EdgeList.Add(x);
            edge.EdgeList.Add(y);
        }

        private static IEnumerable<Edge> Edges(IRoomPlan room)
        {
            Contract.Requires(room != null);
            Contract.Ensures(Contract.Result<IEnumerable<Edge>>() != null);

            return room.Edges().Select((e, i) => new Edge(e.Start, e.End, (uint)i));
        }

        private static Edge GetEdge(IRoomPlan room, uint index)
        {
            Contract.Requires(room != null);

            var e = room.GetEdge(index);
            return new Edge(e.Start, e.End, index);
        }

        private struct Edge
        {
            public readonly List<NeighbourInfo> EdgeList;
            public LineSegment2 Segment;
            public readonly uint Index;

            public Edge(Vector2 a, Vector2 b, uint index)
            {
                Segment = new LineSegment2(a, b);
                Index = index;
                EdgeList = new List<NeighbourInfo>();
            }
        }

        [DebuggerDisplay("T={Pt} R={OtherRoom.Uid} D={Distance}")]
        private class NeighbourInfo
        {
            /// <summary>
            /// distance of point along edge
            /// </summary>
            public float Pt;

            /// <summary>
            /// distance of other point along other edge
            /// </summary>
            public float OPt;

            public float Distance;

            public Vector2 Point;
            public Vector2 OtherPoint;

            public IRoomPlan OtherRoom;

            public uint OtherEdgeIndex;

            public NeighbourInfo NaturalPair;
        }
    }
}
