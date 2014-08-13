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
                //Get a pair of points (a and z)
                var a = edge.EdgeList[i];
                var z = a.NaturalPair;

                //We must have already handled this one if this is true
                // Comparing directly because we only care about if they're *exactly* the same and thus sorting may have reordered them
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (z.Pt < a.Pt || z.Pt == a.Pt)
                // ReSharper restore CompareOfFloatsByEqualityOperator
                    continue;

                //Get all the points between these two
                var between = edge.EdgeList.Skip(i + 1).TakeWhile(item => !ReferenceEquals(item, z));

                if (!between.Any())
                    MaybeAddNeighbour(neighbours, edge.Index, room, a, z);
                else
                {
                    var aPair = between.Append(z).SkipWhile(x => !IsValidPair(a, x)).First();
                    MaybeAddNeighbour(neighbours, edge.Index, room, a, aPair);

                    if (!ReferenceEquals(aPair, z))
                    {
                        var zPair = between.SkipWhile(x => x != aPair).Skip(1).Reverse().SkipWhile(x => !IsValidPair(z, x)).FirstOrDefault();
                        if (zPair != null)
                            MaybeAddNeighbour(neighbours, edge.Index, room, zPair, z);
                    }
                }

            }

            return neighbours;
        }

        private static bool IsValidPair(NeighbourInfo a, NeighbourInfo b)
        {
            return (ReferenceEquals(a.NaturalPair, b)) || (b.Distance <= a.Distance);
        }

        private static void MaybeAddNeighbour(ICollection<FloorPlan.Neighbour> list, uint edgeIndex, FloorPlan.RoomInfo room, NeighbourInfo a, NeighbourInfo b)
        {
            if (Vector2.DistanceSquared(a.Point, b.OtherPoint) < SAME_POINT_EPSILON_SQR)
                return;
            if (Vector2.DistanceSquared(a.OtherPoint, b.Point) < SAME_POINT_EPSILON_SQR)
                return;
            if (Vector2.DistanceSquared(a.Point, b.Point) < SAME_POINT_EPSILON_SQR)
                return;
            if (Vector2.DistanceSquared(a.OtherPoint, b.OtherPoint) < SAME_POINT_EPSILON_SQR)
                return;

            list.Add(new FloorPlan.Neighbour(edgeIndex, room, a.OtherEdgeIndex, a.OtherRoom, b.Point, b.Pt, a.Point, a.Pt, a.OtherPoint, a.OPt, b.OtherPoint, b.OPt));
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
            for (uint i = 0; i < room.InnerFootprint.Length; i++)
                yield return GetEdge(room, i);
        }

        private static Edge GetEdge(FloorPlan.RoomInfo room, uint index)
        {
            return new Edge(
                room.InnerFootprint[index],
                room.InnerFootprint[(index + 1) % room.InnerFootprint.Length],
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
