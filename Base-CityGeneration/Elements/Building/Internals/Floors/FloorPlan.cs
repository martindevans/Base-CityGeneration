using System;
using System.Collections.Generic;
using System.Linq;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    public class FloorPlan
    {
        private const float SCALE = 100000;
        private const float SAME_POINT_EPSILON = 0.1f;
        private const float SAME_POINT_EPSILON_SQR = SAME_POINT_EPSILON * SAME_POINT_EPSILON;

        private bool _isFrozen = false;
        private readonly Clipper _clipper = new Clipper();

        private bool _dirty = true;

        private readonly Vector2[] _footprint;
        private readonly float _externalWallThickness;

        private readonly List<RoomInfo> _rooms = new List<RoomInfo>();
        public IEnumerable<RoomInfo> Rooms
        {
            get { return _rooms; }
        }

        private Dictionary<RoomInfo, List<Neighbour>> _neighbours;

        public FloorPlan(Vector2[] footprint, float externalWallThickness)
        {
            _footprint = footprint;
            _externalWallThickness = externalWallThickness;
        }

        public void Freeze()
        {
            _isFrozen = true;

            GenerateNeighbours();
        }

        public IEnumerable<RoomInfo> AddRoom(IEnumerable<Vector2> roomFootprint, float wallThickness, IEnumerable<ScriptReference> scripts, bool split = false)
        {
            if (_isFrozen)
                throw new InvalidOperationException("Cannot add rooms to floorplan once it is frozen");

            //Contain room inside floor
            _clipper.Clear();
            _clipper.AddPolygon(roomFootprint.Shrink(wallThickness).Select(ToPoint).ToList(), PolyType.Subject);
            _clipper.AddPolygon(_footprint.Shrink(_externalWallThickness).Select(ToPoint).ToList(), PolyType.Clip);
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            _clipper.Execute(ClipType.Intersection, solution);

            if (solution.Count > 1 && !split)
                return new RoomInfo[0];

            //Clip against other rooms
            if (_rooms.Count > 0)
            {
                _clipper.Clear();
                _clipper.AddPolygons(solution, PolyType.Subject);
                _clipper.AddPolygons(_rooms.Select(r => r.Footprint.Shrink(-r.WallThickness).Select(ToPoint).ToList()).ToList(), PolyType.Clip);
                solution.Clear();
                _clipper.Execute(ClipType.Difference, solution, PolyFillType.NonZero, PolyFillType.NonZero);

                if (solution.Count > 1 && !split)
                    return new RoomInfo[0];
            }

            var s = scripts.ToArray();

            List<RoomInfo> result = new List<RoomInfo>();
            foreach (var shape in solution)
            {
                _dirty = true;

                //Ensure shape is still clockwise wound
                if (Clipper.Orientation(shape))
                    shape.Reverse();

                var r  = new RoomInfo(shape.Select(ToVector2).ToArray(), wallThickness, s);
                result.Add(r);
                _rooms.Add(r);
            }

            return result;
        }

        /// <summary>
        /// Generate neighbour data (if the plan is dirty)
        /// </summary>
        private void GenerateNeighbours()
        {
            if (!_dirty)
                return;
            _neighbours = _rooms.ToDictionary(a => a, a => new List<Neighbour>());

            foreach (var room in _rooms)
            {
                //Map points onto this edge
                foreach (var edge in Edges(room))
                {
                    var edgeLine = new Line2D(edge.Segment.Start, edge.Segment.End - edge.Segment.Start);

                    foreach (var otherRoom in _rooms)
                    {
                        if (ReferenceEquals(room, otherRoom))
                            continue;

                        ProjectPointsOntoEdge(otherRoom, edgeLine, edge);
                    }

                    var l = _neighbours[room];
                    l.AddRange(ExtractNeighbourSections(room, edge));
                }
            }

            _dirty = false;
        }

        private IEnumerable<Neighbour> ExtractNeighbourSections(RoomInfo room, Edge edge)
        {
            if (edge.EdgeList.Count == 0)
                return new Neighbour[0];

            //Sort by distance along this edge
            edge.EdgeList.Sort((a, b) => {
                var ret = a.T.CompareTo(b.T);
                if (ret == 0)
                    ret = a.OtherEdgeIndex.CompareTo(b.OtherEdgeIndex);
                return ret;
            });

            //Now we have a load of markers along the edge of this room which mark where the edge of other rooms project onto this edge
            //Walk along list pairing them up

            List<Neighbour> neighbours = new List<Neighbour>();

            for (int i = 0; i < edge.EdgeList.Count; i++)
            {
                //Get a pair of points (a and z)
                var a = edge.EdgeList[i];
                var z = a.NaturalPair;

                //We must have already handled this one if this is true
// Comparing directly because we only care about if they're *exactly* the same and thus sorting may have reordered them
// ReSharper disable CompareOfFloatsByEqualityOperator
                if (z.T < a.T || z.T == a.T)
// ReSharper restore CompareOfFloatsByEqualityOperator
                    continue;

                //Get all the points between these two
                var between = edge.EdgeList.Skip(i + 1).TakeWhile(item => !ReferenceEquals(item, z));

                if (!between.Any())
                    MaybeAddNeighbour(neighbours, room, a, z);
                else
                {
                    var aPair = between.Append(z).SkipWhile(x => !ValidPair(a, x)).First();
                    MaybeAddNeighbour(neighbours, room, a, aPair);

                    if (!ReferenceEquals(aPair, z))
                    {
                        var zPair = between.SkipWhile(x => x != aPair).Skip(1).Reverse().SkipWhile(x => !ValidPair(z, x)).FirstOrDefault();
                        if (zPair != null)
                            MaybeAddNeighbour(neighbours, room, zPair, z);
                    }
                }

            }

            return neighbours;
        }

        private static bool ValidPair(NeighbourInfo a, NeighbourInfo b)
        {
            return (ReferenceEquals(a.NaturalPair, b)) || (b.Distance <= a.Distance);
        }

        private static bool MaybeAddNeighbour(ICollection<Neighbour> list, RoomInfo room, NeighbourInfo a, NeighbourInfo b)
        {
            if (Vector2.DistanceSquared(a.Point, b.OtherPoint) < SAME_POINT_EPSILON_SQR)
                return false;
            if (Vector2.DistanceSquared(a.OtherPoint, b.Point) < SAME_POINT_EPSILON_SQR)
                return false;

            list.Add(new Neighbour(room, a.OtherRoom, a.OtherPoint, b.Point, a.Point, b.OtherPoint));
            return true;
        }

        private static void ProjectPointsOntoEdge(RoomInfo otherRoom, Line2D edgeLine, Edge edge)
        {
            foreach (var otherEdge in Edges(otherRoom))
            {
                var otherEdgeLine = new Line2D(otherEdge.Segment.Start, otherEdge.Segment.End - otherEdge.Segment.Start);

                if (Vector2.Dot(edgeLine.Direction.Perpendicular(), otherEdgeLine.Direction.Perpendicular()) >= 0)
                    continue;

                int lCount = 0;
                if (Geometry2D.IsLeftOfLine(edgeLine, otherEdge.Segment.Start)) lCount++;
                if (Geometry2D.IsLeftOfLine(edgeLine, otherEdge.Segment.End)) lCount++;
                if (Geometry2D.IsLeftOfLine(otherEdgeLine, edge.Segment.Start)) lCount++;
                if (Geometry2D.IsLeftOfLine(otherEdgeLine, edge.Segment.End)) lCount++;
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
                    if (dt < 0)                                                                        //End overlap
                    {
                        var cPoint = edgeLine.Point + edgeLine.Direction * ct;
                        var a = edge.Segment.Start;
                        var at = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, a);
                        var aPoint = otherEdgeLine.Point + otherEdgeLine.Direction * at;

                        CreateNeighbourInfoPair(otherRoom, edge, otherEdge, c, cPoint, ct, a, aPoint, 0);
                    }
                    else if (dt >= 0 && dt <= 1)                                                       //Contained
                    {
                        var cPoint = edgeLine.Point + edgeLine.Direction * ct;
                        var dPoint = edgeLine.Point + edgeLine.Direction * dt;

                        CreateNeighbourInfoPair(otherRoom, edge, otherEdge, c, cPoint, ct, d, dPoint, dt);
                    }
                }
                else
                {
                    if (dt > 0 && dt < 1)                                                              //Start overlap
                    {
                        var dPoint = edgeLine.Point + edgeLine.Direction * dt;
                        var b = edge.Segment.End;
                        var bt = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, b);
                        var bPoint = otherEdgeLine.Point + otherEdgeLine.Direction * bt;

                        CreateNeighbourInfoPair(otherRoom, edge, otherEdge, d, dPoint, dt, b, bPoint, 0);
                    }
                    else if (Math.Sign(ct) != Math.Sign(dt))                                           //Reverse contained
                    {
                        var a = edge.Segment.Start;
                        var at = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, a);
                        var aPoint = otherEdgeLine.Point + otherEdgeLine.Direction * at;

                        var b = edge.Segment.End;
                        var bt = Geometry2D.ClosestPointDistanceAlongLine(otherEdgeLine, b);
                        var bPoint = otherEdgeLine.Point + otherEdgeLine.Direction * bt;

                        CreateNeighbourInfoPair(otherRoom, edge, otherEdge, a, aPoint, 0, b, bPoint, 1);
                    }
                    else
                    {
                                                                                                       //No overlap
                    }
                }
            }
        }

        private static void CreateNeighbourInfoPair(RoomInfo otherRoom, Edge edge,
            Edge otherEdge, Vector2 point1OnEdge, Vector2 point1ProjectedOntoOtherEdge, float point1DistanceAlongEdge,
            Vector2 point2OnEdge, Vector2 point2ProjectedOntoOtherEdge, float point2DistanceAlongEdge)
        {
            var x = new NeighbourInfo
            {
                Distance = Vector2.Distance(point1OnEdge, point1ProjectedOntoOtherEdge),
                Point = point1ProjectedOntoOtherEdge,
                OtherEdgeIndex = otherEdge.Index,
                OtherPoint = point1OnEdge,
                OtherRoom = otherRoom,
                T = point1DistanceAlongEdge
            };

            var y = new NeighbourInfo
            {
                Distance = Vector2.Distance(point2OnEdge, point2ProjectedOntoOtherEdge),
                Point = point2ProjectedOntoOtherEdge,
                OtherEdgeIndex = otherEdge.Index,
                OtherPoint = point2OnEdge,
                OtherRoom = otherRoom,
                T = point2DistanceAlongEdge
            };

            x.NaturalPair = y;
            y.NaturalPair = x;

            edge.EdgeList.Add(x);
            edge.EdgeList.Add(y);
        }

        public IEnumerable<Neighbour> GetNeighbours(RoomInfo room)
        {
            GenerateNeighbours();
            return _neighbours[room];
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

        private static IEnumerable<Edge> Edges(RoomInfo room)
        {
            for (uint i = 0; i < room.Footprint.Length; i++)
                yield return GetEdge(room, i);
        }

        private static Edge GetEdge(RoomInfo room, uint index)
        {
            return new Edge(
                room.Footprint[index],
                room.Footprint[(index + 1) % room.Footprint.Length],
                index
            );
        }
        #endregion

        public class RoomInfo
        {
            public readonly Vector2[] Footprint;
            public readonly ScriptReference[] Scripts;
            public readonly float WallThickness;

            public object Tag;

            public RoomInfo(Vector2[] footprint, float wallThickness, ScriptReference[] scripts)
            {
                Footprint = footprint;
                Scripts = scripts;
                WallThickness = wallThickness;
            }
        }

        public class Neighbour
        {
            public RoomInfo RoomAB { get; private set; }
            public RoomInfo RoomCD { get; private set; }

            public Vector2 A { get; private set; }
            public Vector2 B { get; private set; }
            public Vector2 C { get; private set; }
            public Vector2 D { get; private set; }

            public Neighbour(RoomInfo ab, RoomInfo cd, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                RoomAB = ab;
                RoomCD = cd;
                A = a;
                B = b;
                C = c;
                D = d;
            }
        }

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
            public float T;
            public float Distance;

            public Vector2 Point;
            public Vector2 OtherPoint;

            public RoomInfo OtherRoom;

            public uint OtherEdgeIndex;

            public NeighbourInfo NaturalPair;
        }
    }
}
