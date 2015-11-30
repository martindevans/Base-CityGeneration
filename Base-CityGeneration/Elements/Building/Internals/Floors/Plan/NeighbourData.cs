using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    internal class NeighbourData
    {
        #region fields/properties
        internal const float SAME_POINT_EPSILON = 0.1f;
        internal const float SAME_POINT_EPSILON_SQR = SAME_POINT_EPSILON * SAME_POINT_EPSILON;

        private readonly FloorPlan _plan;

        public bool Dirty { get; set; }

        private Dictionary<RoomPlan, List<FloorPlan.Neighbour>> _neighbours;

        public IEnumerable<FloorPlan.Neighbour> this[RoomPlan key]
        {
            get { return _neighbours[key]; }
        }
        #endregion

        public NeighbourData(FloorPlan plan)
        {
            _plan = plan;

            Dirty = true;
        }

        public void GenerateNeighbours()
        {
            if (!Dirty)
                return;

            _neighbours = _plan.Rooms.ToDictionary(a => a, a => new List<FloorPlan.Neighbour>());

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

        private static IEnumerable<FloorPlan.Neighbour> ExtractNeighbourSections(RoomPlan room, Edge edge)
        {
            if (edge.EdgeList.Count == 0)
                return new FloorPlan.Neighbour[0];

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

            List<FloorPlan.Neighbour> neighbours = new List<FloorPlan.Neighbour>();

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
                List<FloorPlan.Neighbour> segmentNeighbours = new List<FloorPlan.Neighbour>();
                AddNeighbour(segmentNeighbours, edge.Index, room, a, b);

                //Narrow down segment by slicing out parts where an occluding overlap occurs
                for (int j = 0; j < overlaps.Length; j++)
                {
                    var o1 = overlaps[j];
                    var o2 = o1.NaturalPair;

                    for (int k = 0; k < segmentNeighbours.Count; k++)
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
            return a.Pt <= point.Pt && b.Pt >= point.Pt;
        }

        private static void AddNeighbour_N_To_Info(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, RoomPlan room, FloorPlan.Neighbour n, bool nA, NeighbourInfo info)
        {
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

        private static NeighbourInfo ToNeighbourInfoAD(FloorPlan.Neighbour n)
        {
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

        private static NeighbourInfo ToNeighbourInfoBC(FloorPlan.Neighbour n)
        {
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

        private static void AddNeighbour(ICollection<FloorPlan.Neighbour> list, uint edgeIndex, RoomPlan room, NeighbourInfo a, NeighbourInfo b)
        {
            //Swap points if order is reversed
            if (a.Pt > b.Pt)
            {
                var t = a;
                a = b;
                b = t;
            }

            if (Vector2.Distance(a.Point, b.Point) < 0.05f)
                return;

            list.Add(new FloorPlan.Neighbour(edgeIndex, room, a.OtherEdgeIndex, a.OtherRoom,
                a.Point, a.Pt,
                b.Point, b.Pt,
                b.OtherPoint, b.OPt,
                a.OtherPoint, a.OPt
            ));
        }

        private static void ProjectPointsOntoEdge(RoomPlan otherRoom, Ray2 edgeLine, Edge edge)
        {
            foreach (var otherEdge in Edges(otherRoom))
            {
                var otherEdgeLine = new Ray2(otherEdge.Segment.Start, otherEdge.Segment.End - otherEdge.Segment.Start);

                if (Vector2.Dot(edgeLine.Direction.Perpendicular(), otherEdgeLine.Direction.Perpendicular()) >= 0)
                    continue;

                int lCount = 0;
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

                if (ct < dt)
                    throw new InvalidOperationException("Edge is wound incorrectly");

                if (ct >= 0 && ct <= 1)
                {
                    if (dt < 0)
                    {
                        // End Overlap
                        //      B------------A
                        //               C-------D
                        var at = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.Start);

                        CreateNeighbourInfoPair(otherRoom, edge, ct, 0, otherEdge, 0, at);
                    }
                    else if (dt >= 0 && dt <= 1)
                    {
                        // Contained:
                        //      B------------A
                        //         C-------D
                        CreateNeighbourInfoPair(otherRoom, edge, ct, 0, otherEdge, dt, 1);
                    }
                }
                else
                {
                    if (dt > 0 && dt < 1)
                    {
                        // Start Overlap
                        //      B------------A
                        // C-------D
                        var bt = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.End);

                        CreateNeighbourInfoPair(otherRoom, edge, dt, 1, otherEdge, 1, bt);
                    }
                    else if (Math.Sign(ct) != Math.Sign(dt))
                    {
                        // Reverse Contained:
                        //      B------------A
                        // C---------------------D
                        var at = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.Start);
                        var bt = otherEdgeLine.ClosestPointDistanceAlongLine(edge.Segment.End);

                        CreateNeighbourInfoPair(otherRoom, edge, 0, at, otherEdge, 1, bt);
                    }
                    else
                    {
                        //No overlap
                    }
                }
            }
        }

        private static void CreateNeighbourInfoPair(RoomPlan otherRoom,
            Edge edge, float point1DistanceAlongEdge, float point1ProjDistanceAlongOtherEdge,
            Edge otherEdge, float point2DistanceAlongEdge, float point2ProjDistanceAlongOtherEdge)
        {
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

        private static IEnumerable<Edge> Edges(RoomPlan room)
        {
            return room.Edges().Select((e, i) => new Edge(e.Start, e.End, (uint)i));
        }

        private static Edge GetEdge(RoomPlan room, uint index)
        {
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

        [DebuggerDisplay("T={Pt} R={OtherRoom.Id} D={Distance}")]
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

            public RoomPlan OtherRoom;

            public uint OtherEdgeIndex;

            public NeighbourInfo NaturalPair;
        }
    }
}
