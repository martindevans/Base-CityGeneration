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

            List<Watermark> watermarks = new List<Watermark>();
            for (int i = 0; i < edge.EdgeList.Count; i++)
            {
                //Get a pair of points (a and z)
                var a = edge.EdgeList[i];
                var z = a.NaturalPair;

                //We must have already handled this one if this is true
                if (z.Pt <= a.Pt)
                    continue;

                //If any other pairs have established a lower watermark beyond this point then give up
                if (watermarks.Any(x => x.T > a.Pt && x.Height < a.Distance))
                    continue;

                //Select the next logical end point
                var end = edge.EdgeList.Skip(i + 1).FirstOrDefault(x => IsValidPair(a, x)) ?? z;

                if (MaybeAddNeighbour(neighbours, edge.Index, room, a, end))
                    watermarks.Add(new Watermark {Height = end.Distance, T = end.Pt});
            }

            return neighbours;
        }

        private struct Watermark
        {
            public float Height;
            public float T;
        }

        private static bool IsValidPair(NeighbourInfo a, NeighbourInfo b)
        {
            return (ReferenceEquals(a.NaturalPair, b)) || (
                (b.Distance <= a.Distance)
            );
        }

        private static bool MaybeAddNeighbour(ICollection<FloorPlan.Neighbour> list, uint edgeIndex, FloorPlan.RoomInfo room, NeighbourInfo a, NeighbourInfo b)
        {
            var ap = a.Point;
            var apt = a.Pt;

            var aop = a.OtherPoint;
            var aopt = a.OPt;

            var bp = b.Point;
            var bpt = b.Pt;

            var bop = b.OtherPoint;
            var bopt = b.OPt;

            if (a.OtherRoom != b.OtherRoom)
            {
                var otherRoom = a.Distance > b.Distance ? a.OtherRoom : b.OtherRoom;
                var otherEdge = a.Distance > b.Distance ? GetEdge(a.OtherRoom, a.OtherEdgeIndex) : GetEdge(b.OtherRoom, b.OtherEdgeIndex);

                var f = Geometry2D.ClosestPointDistanceAlongLine(new Line2D(otherEdge.Segment.Start, otherEdge.Segment.End - otherEdge.Segment.Start), b.OtherPoint);

                bop = otherEdge.Segment.Start + (otherEdge.Segment.End - otherEdge.Segment.Start) * f;
                bopt = f;
            }

            if (Vector2.DistanceSquared(ap, bop) < SAME_POINT_EPSILON_SQR)
                return false;
            if (Vector2.DistanceSquared(aop, bp) < SAME_POINT_EPSILON_SQR)
                return false;
            if (Vector2.DistanceSquared(ap, bp) < SAME_POINT_EPSILON_SQR)
                return false;
            if (Vector2.DistanceSquared(aop, bop) < SAME_POINT_EPSILON_SQR)
                return false;

            list.Add(new FloorPlan.Neighbour(edgeIndex, room, a.OtherEdgeIndex, a.OtherRoom, bp, bpt, ap, apt, aop, aopt, bop, bopt));
            return true;
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
