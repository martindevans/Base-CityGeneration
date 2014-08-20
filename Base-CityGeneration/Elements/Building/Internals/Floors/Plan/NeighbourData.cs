using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Plan
{
    internal class NeighbourData
    {
        #region fields/properties
        private const float SAME_POINT_EPSILON = 0.1f;
        private const float SAME_POINT_EPSILON_SQR = SAME_POINT_EPSILON * SAME_POINT_EPSILON;

        private readonly FloorPlan _plan;

        public bool Dirty { get; set; }

        private Dictionary<FloorPlan.RoomInfo, List<FloorPlan.Neighbour>> _neighbours;

        public IEnumerable<FloorPlan.Neighbour> this[FloorPlan.RoomInfo key]
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
                    var edgeLine = new Line2D(edge.Segment.Start, edge.Segment.End - edge.Segment.Start);

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

        private static IEnumerable<FloorPlan.Neighbour> ExtractNeighbourSections(FloorPlan.RoomInfo room, Edge edge)
        {
            if (edge.EdgeList.Count == 0)
                return new FloorPlan.Neighbour[0];

            //Sort by distance along this edge
            edge.EdgeList.Sort((a, b) =>
            {
                var ret = a.Pt.CompareTo(b.Pt);
                if (ret == 0)
                    ret = a.OtherEdgeIndex.CompareTo(b.OtherEdgeIndex);
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

                //No overlaps, we can add this section straight in and move on
                if (!neighbours.Any(n => (n.At < b.Pt && n.Bt > a.Pt)))
                {
                    AddNeighbour(neighbours, edge.Index, room, a, b);
                }
                else
                {
                    //Iterate backwards over existing neighbour segments
                    for (int j = neighbours.Count - 1; j >= 0; j--)
                    {
                        var n = neighbours[j];

                        if (n.At < b.Pt && n.At >= a.Pt && n.Bt <= b.Pt && n.Bt > a.Pt)
                        {
                            //CONTAINS OVERLAP
                            //e.g. AB totally contains n
                            //  n.At == 0
                            //  n.Bt == 0.5
                            //  a.Pt == 0
                            //  b.Pt == 1

                            if (a.Distance < Vector2.Distance(n.A, n.C))
                            {
                                //We need to simply remove and replace the neighbour section entirely since it's totally occluded by this new section
                                neighbours.RemoveAt(j);
                                AddNeighbour(neighbours, edge.Index, room, a, b);
                            }
                            else
                            {
                                //We need to split the new section around the neighbour section

                                //Create section from a -> n.A
                                if (Math.Abs(a.Pt - n.At) > float.Epsilon)
                                {
                                    throw new NotImplementedException();
                                }

                                //Create section from n.B -> b
                                if (Math.Abs(n.Bt - b.Pt) > float.Epsilon)
                                {
                                    AddNeighbour_Info_To_NB(neighbours, edge.Index, room, n, b);
                                }
                            }
                        }
                        else if (n.Bt <= b.Pt && n.Bt > a.Pt)
                        {
                            //START OVERLAP
                            //e.g. Start of AB overlaps n
                            //  n.At == 0
                            //  n.Bt == 1
                            //  a.Pt == 0.5
                            //  b.Pt == 1

                            if (a.Distance < Vector2.Distance(n.A, n.D))
                            {
                                //Remove this neighbour, we need to modify it
                                neighbours.RemoveAt(j);

                                //Create 2 new neighbour sections, all of AB (since it's closest) and what's left of N
                                AddNeighbour_NA_To_Info(neighbours, edge.Index, room, n, a);
                                AddNeighbour(neighbours, edge.Index, room, a, b);
                            }
                            else
                            {
                                //Create 1 new neighbour sections, all of N (since it's closest) and what's left of AB
                                AddNeighbour_Info_To_NB(neighbours, edge.Index, room, n, b);
                            }
                        }
                        else if (n.At <= a.Pt && n.Bt >= b.Pt)
                        {
                            //Other section completely contains this section
                            //we either need to skip this section (it's entirely occluded) or split other section
                            if (a.Distance < Vector2.Distance(n.A, n.C))
                            {
                                //Remove the section we're modifying
                                neighbours.RemoveAt(j);

                                //Create new section
                                AddNeighbour(neighbours, edge.Index, room, a, b);

                                //Create section from n.A -> a (reproj)
                                if (Math.Abs(a.Pt - n.At) > float.Epsilon)
                                    AddNeighbour_NA_To_Info(neighbours, edge.Index, room, n, a);

                                //Create section from b (reproj) -> n.B
                                if (Math.Abs(n.Bt - b.Pt) > float.Epsilon)
                                    AddNeighbour_NB_To_Info(neighbours, edge.Index, room, n, b);
                            }
                            else
                            {
                                //Entirely occluded section - do nothing
                            }
                        }
                        else
                            throw new NotImplementedException("Unhandled case?");
                    }
                }
            }

            return neighbours;
        }

        private static void AddNeighbour_N_To_Info(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, FloorPlan.RoomInfo room, FloorPlan.Neighbour n, bool nA, NeighbourInfo info)
        {
            var lineOut = new Line2D(info.Point, info.OtherPoint - info.Point);
            var otherEdge = GetEdge(n.RoomCD, n.EdgeIndexRoomCD).Segment;
            var otherEdgeLine = new Line2D(otherEdge.Start, otherEdge.End - otherEdge.Start);

            var proj = Geometry2D.LineLineIntersection(lineOut, otherEdgeLine);
            if (!proj.HasValue)
                throw new InvalidOperationException("Reprojected segment section does not lie on other edge");

            AddNeighbour(neighbours, edgeIndex, room, nA ? ToNeighbourInfoAD(n) : ToNeighbourInfoBC(n), new NeighbourInfo
            {
                Distance = nA ? Vector2.Distance(n.A, n.D) : Vector2.Distance(n.B, n.C),
                NaturalPair = null,
                Point = info.Point,
                Pt = info.Pt,
                OtherPoint = proj.Value.Position,
                OPt = proj.Value.DistanceAlongLineB,
                OtherRoom = n.RoomCD,
                OtherEdgeIndex = n.EdgeIndexRoomCD
            });
        }

        private static void AddNeighbour_NA_To_Info(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, FloorPlan.RoomInfo room, FloorPlan.Neighbour n, NeighbourInfo info)
        {
            AddNeighbour_N_To_Info(neighbours, edgeIndex, room, n, true, info);
        }

        private static void AddNeighbour_NB_To_Info(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, FloorPlan.RoomInfo room, FloorPlan.Neighbour n, NeighbourInfo info)
        {
            AddNeighbour_N_To_Info(neighbours, edgeIndex, room, n, false, info);
        }

        private static void AddNeighbour_Info_To_N(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, FloorPlan.RoomInfo room, NeighbourInfo info, FloorPlan.Neighbour n, bool nA)
        {
            var lineOut = nA ? new Line2D(n.A, n.D - n.A) : new Line2D(n.B, n.C - n.B);
            var otherEdge = GetEdge(info.OtherRoom, info.OtherEdgeIndex).Segment;
            var otherEdgeLine = new Line2D(otherEdge.Start, otherEdge.End - otherEdge.Start);

            var proj = Geometry2D.LineLineIntersection(lineOut, otherEdgeLine);
            if (!proj.HasValue)
                throw new InvalidOperationException("Reprojected segment section does not lie on other edge");

            AddNeighbour(neighbours, edgeIndex, room, new NeighbourInfo
            {
                Distance = info.Distance,
                NaturalPair = null,
                OtherEdgeIndex = info.OtherEdgeIndex,
                Point = nA ? n.A : n.B,
                Pt = nA ? n.At : n.Bt,
                OtherPoint = proj.Value.Position,
                OPt = proj.Value.DistanceAlongLineB,
                OtherRoom = info.OtherRoom
            }, info);
        }

        private static void AddNeighbour_Info_To_NA(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, FloorPlan.RoomInfo room, FloorPlan.Neighbour n, NeighbourInfo info)
        {
            AddNeighbour_Info_To_N(neighbours, edgeIndex, room, info, n, true);
        }

        private static void AddNeighbour_Info_To_NB(ICollection<FloorPlan.Neighbour> neighbours, uint edgeIndex, FloorPlan.RoomInfo room, FloorPlan.Neighbour n, NeighbourInfo info)
        {
            AddNeighbour_Info_To_N(neighbours, edgeIndex, room, info, n, false);
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

        private static void AddNeighbour(ICollection<FloorPlan.Neighbour> list, uint edgeIndex, FloorPlan.RoomInfo room, NeighbourInfo a, NeighbourInfo b)
        {
            //Swap points if order is reversed
            if (a.Pt > b.Pt)
            {
                var t = a;
                a = b;
                b = t;
            }

            var ap = a.Point;
            var apt = a.Pt;

            var aop = a.OtherPoint;
            var aopt = a.OPt;

            var bp = b.Point;
            var bpt = b.Pt;

            var bop = b.OtherPoint;
            var bopt = b.OPt;

            if (Vector2.DistanceSquared(ap, bop) < SAME_POINT_EPSILON_SQR)
                throw new ArgumentException("Points A.Point and B.Other are the same", "a");
            if (Vector2.DistanceSquared(aop, bp) < SAME_POINT_EPSILON_SQR)
                throw new ArgumentException("Points A.Other and B.Point are the same", "a");
            if (Vector2.DistanceSquared(ap, bp) < SAME_POINT_EPSILON_SQR)
                throw new ArgumentException("Points A.Point and B.Point are the same", "a");
            if (Vector2.DistanceSquared(aop, bop) < SAME_POINT_EPSILON_SQR)
                throw new ArgumentException("Points A.Other and B.Other are the same", "a");

            //throw new NotImplementedException("Check and fix winding");

            list.Add(new FloorPlan.Neighbour(edgeIndex, room, a.OtherEdgeIndex, a.OtherRoom,
                ap, apt,
                bp, bpt,
                bop, bopt,
                aop, aopt
            ));

            var last = list.Last();
            if (new Vector2[] {last.A, last.B, last.C, last.D}.Area() > 0)
            {
            }
        }

        private static void ProjectPointsOntoEdge(FloorPlan.RoomInfo otherRoom, Line2D edgeLine, Edge edge)
        {
            foreach (var otherEdge in Edges(otherRoom))
            {
                var otherEdgeLine = new Line2D(otherEdge.Segment.Start, otherEdge.Segment.End - otherEdge.Segment.Start);

                if (Vector2.Dot(edgeLine.Direction.Perpendicular(), otherEdgeLine.Direction.Perpendicular()) >= 0)
                    continue;

                int lCount = 0;
                if (Geometry2D.IsLeftOfLine(edgeLine, otherEdge.Segment.Start))
                    lCount++;
                if (Geometry2D.IsLeftOfLine(edgeLine, otherEdge.Segment.End))
                    lCount++;
                if (Geometry2D.IsLeftOfLine(otherEdgeLine, edge.Segment.Start))
                    lCount++;
                if (Geometry2D.IsLeftOfLine(otherEdgeLine, edge.Segment.End))
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
                var ct = Geometry2D.ClosestPointDistanceAlongLine(edgeLine, c);
                var d = otherEdge.Segment.End;
                var dt = Geometry2D.ClosestPointDistanceAlongLine(edgeLine, d);

                if (ct < dt)
                    throw new InvalidOperationException("Edge is wound incorrectly");

                if (ct >= 0 && ct <= 1)
                {
                    if (dt < 0)
                    {
                        // End Overlap
                        //      B------------A
                        //               C-------D
                        var at = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, edge.Segment.Start);

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
                        var bt = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, edge.Segment.End);

                        CreateNeighbourInfoPair(otherRoom, edge, dt, 1, otherEdge, 1, bt);
                    }
                    else if (Math.Sign(ct) != Math.Sign(dt))
                    {
                        // Reverse Contained:
                        //      B------------A
                        // C---------------------D
                        var at = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, edge.Segment.Start);
                        var bt = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, edge.Segment.End);

                        CreateNeighbourInfoPair(otherRoom, edge, 0, at, otherEdge, 1, bt);
                    }
                    else
                    {
                        //No overlap
                    }
                }
            }
        }

        private static void CreateNeighbourInfoPair(FloorPlan.RoomInfo otherRoom,
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

        #region static helpers
        private static IEnumerable<Edge> Edges(FloorPlan.RoomInfo room)
        {
            for (uint i = 0; i < room.OuterFootprint.Length; i++)
                yield return GetEdge(room, i);
        }

        private static Edge GetEdge(FloorPlan.RoomInfo room, uint index)
        {
            return new Edge(
                room.OuterFootprint[index],
                room.OuterFootprint[(index + 1) % room.OuterFootprint.Length],
                index
            );
        }
        #endregion

        private struct Edge
        {
            public readonly List<NeighbourInfo> EdgeList;
            public LineSegment2D Segment;
            public readonly uint Index;

            public Edge(Vector2 a, Vector2 b, uint index)
            {
                Segment = new LineSegment2D(a, b);
                Index = index;
                EdgeList = new List<NeighbourInfo>();
            }
        }

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

            public FloorPlan.RoomInfo OtherRoom;

            public uint OtherEdgeIndex;

            public NeighbourInfo NaturalPair;
        }
    }
}
