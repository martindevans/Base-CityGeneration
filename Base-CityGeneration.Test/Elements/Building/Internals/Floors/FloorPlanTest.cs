using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric;
using Base_CityGeneration.TestHelpers;
using Myre.Collections;
using SwizzleMyVectors;
using SwizzleMyVectors.Geometry;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors
{
    [TestClass]
    public class FloorPlanTest
    {
        private IFloorPlanBuilder _plan;

        [TestInitialize]
        public void Initialize()
        {
            _plan = new GeometricFloorplan(
                new ReadOnlyCollection<Vector2>(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(100, 100), new Vector2(100, -100) })
            );
        }

        [TestMethod]
        public void RoomInternalBordersAreSmaller()
        {
            var r = _plan.Add(new Vector2[]
            {
                new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10)
            }, 0.1f).Single();

            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(-9.9f, 9.9f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(-9.9f, -9.9f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(9.9f, 9.9f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(9.9f, -9.9f), 0.1f));
        }

        [TestMethod]
        public void RoomInternalBordersAreSmallerWhenNotAtOrigin()
        {
            var r = _plan.Add(new Vector2[]
            {
                new Vector2(10, 10), new Vector2(10, 30), new Vector2(30, 30), new Vector2(30, 10)
            }, 0.1f).Single();

            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(10.11f, 29.889f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(10.11f, 10.11f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(29.899f, 10.11f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(29.889f, 10.11f), 0.1f));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CannotAddToFloorPlanAfterFreeze()
        {
            _plan.Freeze();
            _plan.Add(new Vector2[0], 0.1f);
        }

        [TestMethod]
        public void AddNewRoomToEmptyFloorPlanSucceeds()
        {
            Assert.IsTrue(_plan.Add(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f).Any()
            );
        }

        [TestMethod]
        public void AddNewSplitRoomsFails()
        {
            Assert.IsTrue(_plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f).Any()
            );

            Assert.IsFalse(_plan.Add(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, -100) },
                0.1f).Any()
            );
        }

        [TestMethod]
        public void AddNewSplitRoomsSucceeds()
        {
            Assert.IsTrue(_plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f).Any()
            );

            Assert.AreEqual(2,
                _plan.Add(
                    new Vector2[] { new Vector2(-10, -100), new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, -100) },
                    0.1f,
                    true
                ).Count()
            );
        }

        [TestMethod]
        public void AddNewRoomOutsideFloorBoundsIsClipped()
        {
            Assert.IsTrue(_plan.Add(
                new Vector2[] { new Vector2(-200, -10), new Vector2(-200, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f).Any()
            );
        }

        [TestMethod]
        public void ExactlyMirroredRoomsAreNeighbours()
        {
            var a = _plan.Add(new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(0, 10), new Vector2(0, -10) }, 1).Single();
            var b = _plan.Add(new Vector2[] { new Vector2(0, -10), new Vector2(0, 10), new Vector2(10, 10), new Vector2(10, -10) }, 1).Single();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());

            Assert.IsTrue(a.Neighbours.Any(n => n.Other(a) == b));
        }

        [TestMethod]
        public void NeighboursOfSingleRoomAreNone()
        {
            var r = _plan.Add(
                new Vector2[] { new Vector2(-200, -10), new Vector2(-200, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f).Single();

            Assert.IsFalse(r.Neighbours.Any());
        }

        [TestMethod]
        [Timeout(1000)]
        public void GetRoomNeighboursFindsBasicPair()
        {
            //A really wide room
            var wide = _plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f
            ).Single();

            //Low room
            var low = _plan.Add(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.AreEqual(1, wide.Neighbours.Count());

            //Check that points C and D lies on the edge of low room
            var n = wide.Neighbours.Single(a => a.RoomCD == low);
            Assert.IsTrue(Math.Abs(new Ray2(new Vector2(-10, -90f), new Vector2(1, 0)).DistanceToPoint(n.C)) < 0.1f);
            Assert.IsTrue(Math.Abs(new Ray2(new Vector2(-10, -90f), new Vector2(1, 0)).DistanceToPoint(n.D)) < 0.1f);

            //Check that points A and B lie on the edge of wide room
            Assert.IsTrue(Math.Abs(new Ray2(new Vector2(-10, -10f), new Vector2(1, 0)).DistanceToPoint(n.A)) < 0.1f);
            Assert.IsTrue(Math.Abs(new Ray2(new Vector2(-10, -10f), new Vector2(1, 0)).DistanceToPoint(n.B)) < 0.1f);

            //Check that neighbour data is the same going the other direction
            Assert.AreEqual(1, low.Neighbours.Count());
            Assert.IsTrue(low.Neighbours.Any(a => a.RoomCD == wide));

            //Check that point is close to the edge it is supposed to lie on
            var segment = new LineSegment2(wide.OuterFootprint[(int)n.EdgeIndexRoomAB], wide.OuterFootprint[(int)(n.EdgeIndexRoomAB + 1) % wide.OuterFootprint.Count]);
            var line = segment.Line;
            var dist = line.DistanceToPoint(n.A);
            Assert.IsTrue(dist < 0.01f);

            //Check that point is close to the edge it is supposed to lie on
            var segment2 = new LineSegment2(low.OuterFootprint[(int)n.EdgeIndexRoomCD], low.OuterFootprint[(int)(n.EdgeIndexRoomCD + 1) % low.OuterFootprint.Count]);
            var line2 = segment2.Line;
            var dist2 = line2.DistanceToPoint(n.C);
            Assert.IsTrue(dist2 < 0.01f);

            //Check that distance along edge is correct for points
            Assert.IsTrue(Vector2.Distance(n.A, segment.Start + (segment.End - segment.Start) * n.At) < 0.1f);
            Assert.IsTrue(Vector2.Distance(n.B, segment.Start + (segment.End - segment.Start) * n.Bt) < 0.1f);
            Assert.IsTrue(Vector2.Distance(n.C, segment2.Start + (segment2.End - segment2.Start) * n.Ct) < 0.1f);
            Assert.IsTrue(Vector2.Distance(n.D, segment2.Start + (segment2.End - segment2.Start) * n.Dt) < 0.1f);
        }

        [TestMethod]
        public void RoomNeighbourInfoIsCorrectlyWound_AntiClockwise()
        {
            //A really wide room
            var wide = _plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f
            ).Single();

            //High room
            var high = _plan.Add(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.IsNotNull(wide);
            Assert.IsNotNull(high);

            var wideNeighbours = wide.Neighbours.Single();

            Assert.IsTrue(new[] { wideNeighbours.A, wideNeighbours.B, wideNeighbours.C, wideNeighbours.D }.Area() < 0);
        }

        [TestMethod]
        [Timeout(1000)]
        public void GetRoomNeighboursFindsBasicPairReversed()
        {
            //A really wide room
            var wide = _plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f
            ).Single();

            //High room
            var high = _plan.Add(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.AreEqual(1, wide.Neighbours.Count());
            Assert.IsTrue(wide.Neighbours.Any(a => a.RoomCD == high));

            Assert.AreEqual(1, high.Neighbours.Count());
            Assert.IsTrue(high.Neighbours.Any(a => a.RoomCD == wide));
        }

        [TestMethod]
        public void StartOverlapRoomsAreHandled()
        {
            //      /-----\
            //      |  A  |
            //      \-----/
            //
            //  /------\
            //  |  B   |
            //  \------/
            //
            //      |--|
            // Overlap from X: -10 -> 0


            var a = _plan.Add(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f
            ).Single();

            var b = _plan.Add(
                new Vector2[] { new Vector2(-15, -30), new Vector2(-15, -20), new Vector2(0, -20), new Vector2(0, -30) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.AreEqual(1, a.Neighbours.Count());
            var n1 = a.Neighbours.Single(x => x.RoomCD == b);
            var n2 = a.Neighbours.Single(x => x.RoomAB == a);

            Assert.AreEqual(n1, n2);

            Assert.IsTrue(a.OuterFootprint.Any(p => Vector2.Distance(p, n1.B) < 0.1f));
            Assert.IsTrue(b.OuterFootprint.Any(p => Vector2.Distance(p, n1.D) < 0.1f));
        }

        [TestMethod]
        public void DisjointRoomsAreHandled()
        {
            //A really wide room
            var a = _plan.Add(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f
            ).Single();

            var b = _plan.Add(
                new Vector2[] { new Vector2(15, -30), new Vector2(15, -20), new Vector2(20, -20), new Vector2(20, -30) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);

            Assert.AreEqual(0, a.Neighbours.Count());
        }

        [TestMethod]
        public void RoomsOccludeFartherRoomsFromBeingNeighbours()
        {
            //Low room
            var low = _plan.Add(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f
            ).Single();

            //High room
            var high = _plan.Add(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f
            ).Single();

            //A really wide room (which should occlude low and high from being neighbours)
            var wide = _plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.AreEqual(2, wide.Neighbours.Count());
            Assert.IsTrue(wide.Neighbours.Any(a => a.RoomCD == low));
            Assert.IsTrue(wide.Neighbours.Any(a => a.RoomCD == high));

            Assert.AreEqual(1, low.Neighbours.Count());
            Assert.IsTrue(low.Neighbours.Any(a => a.RoomCD == wide));

            Assert.AreEqual(1, high.Neighbours.Count());
            Assert.IsTrue(high.Neighbours.Any(a => a.RoomCD == wide));
        }

        [TestMethod]
        public void GetRoomNeighboursFindsNeighbours()
        {
            //A really wide room
            var wide = _plan.Add(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f
            ).Single();

            //Low room
            var low = _plan.Add(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f
            ).Single();

            //High room
            var high = _plan.Add(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f
            ).Single();

            //High left
            var highLeft = _plan.Add(
                new Vector2[] { new Vector2(-100, 100), new Vector2(-90, 100), new Vector2(-90, 90), new Vector2(-100, 90) },
                0.1f
            ).Single();

            _plan.Freeze();

            Assert.AreEqual(3, wide.Neighbours.Count());
            Assert.IsTrue(wide.Neighbours.Any(a => a.RoomCD == low));
            Assert.IsTrue(wide.Neighbours.Any(a => a.RoomCD == high));
            Assert.IsTrue(wide.Neighbours.Any(a => a.RoomCD == highLeft));

            Assert.AreEqual(1, low.Neighbours.Count());
            Assert.IsTrue(low.Neighbours.Any(a => a.RoomCD == wide));

            Assert.AreEqual(2, high.Neighbours.Count());
            Assert.IsTrue(high.Neighbours.Any(a => a.RoomCD == wide));
            Assert.IsTrue(high.Neighbours.Any(a => a.RoomCD == highLeft));
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesExternalSections()
        {
            var room = _plan.Add(_plan.ExternalFootprint, 0.25f).Single();

            _plan.Freeze();

            var facades = room.GetWalls().ToArray();

            Assert.AreEqual(8, facades.Length);
            Assert.AreEqual(4, facades.Count(f => f.Section.IsCorner));
            Assert.AreEqual(4, facades.Count(f => !f.Section.IsCorner));
            Assert.IsTrue(facades.All(f => f.IsExternal));
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesExternalSectionsForHalfExternalRoom()
        {
            var room = _plan.Add(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 0.25f).Single();

            _plan.Freeze();

            var facades = room.GetWalls().ToArray();

            Assert.AreEqual(4, facades.Count(f => f.Section.IsCorner));
            Assert.AreEqual(7, facades.Count(f => f.IsExternal));
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesInternalSections()
        {
            var room = _plan.Add(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 0.25f).Single();

            _plan.Freeze();

            var facades = room.GetWalls().ToArray();

            Assert.AreEqual(4, facades.Count(f => !f.Section.IsCorner));
            Assert.AreEqual(1, facades.Count(f => !f.IsExternal));
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesNeighbourSections()
        {
            var roomA = _plan.Add(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 3.25f).Single();
            var roomB = _plan.Add(new Vector2[] { new Vector2(10, -10), new Vector2(10, 10), new Vector2(20, 10), new Vector2(20, -10) }, 3.25f).Single();

            _plan.Freeze();

            var facades = roomA.GetWalls().ToArray();

            var internalFacades = facades.Where(f => !f.IsExternal).ToArray();

            Assert.AreEqual(1, internalFacades.Count(f => f.NeighbouringRoom == roomB));
            Assert.AreEqual(3, internalFacades.Length);

            //Check that the neighbour section has points lying on both rooms
            var n = internalFacades.Single(f => f.NeighbouringRoom == roomB);
            Assert.IsTrue(roomB.Edges().Any(e => e.DistanceToPoint(n.Section.C) <= 3.25f));
            Assert.IsTrue(roomA.Edges().Any(e => e.DistanceToPoint(n.Section.A) <= 3.25f));

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        private void AssertAllWindings()
        {
            foreach (var roomInfo in _plan.Rooms)
            {
                foreach (var neighbour in roomInfo.Neighbours)
                {
                    Assert.IsTrue(new Vector2[] { neighbour.A, neighbour.B, neighbour.C, neighbour.D }.Area() < 0);
                }
            }
        }

        private void AssertAllSections()
        {
            Func<IRoomPlan, LineSegment2[]> edges = r => r.OuterFootprint.Select((a, i) => new LineSegment2(a, r.OuterFootprint[(i + 1) % r.OuterFootprint.Count])).ToArray();

            foreach (var neighbour in _plan.Rooms.SelectMany(roomInfo => roomInfo.Neighbours))
            {
                Assert.IsTrue(edges(neighbour.RoomAB).Any(e => e.DistanceToPoint(neighbour.A) < 0.1f));
                Assert.IsTrue(edges(neighbour.RoomAB).Any(e => e.DistanceToPoint(neighbour.B) < 0.1f));
                Assert.IsTrue(edges(neighbour.RoomCD).Any(e => e.DistanceToPoint(neighbour.C) < 0.1f));
                Assert.IsTrue(edges(neighbour.RoomCD).Any(e => e.DistanceToPoint(neighbour.D) < 0.1f));
            }
        }

        [TestMethod]
        public void Floorplan_SeparateRooms_HaveNoNeighbours()
        {
            var roomA = _plan.Add(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, -80), new Vector2(-80, -80), new Vector2(-80, -100) }, 3f).Single();
            var roomB = _plan.Add(new Vector2[] { new Vector2(100, 100), new Vector2(100, 80), new Vector2(80, 80), new Vector2(80, 100) }, 3f).Single();

            _plan.Freeze();

            Assert.AreEqual(0, roomA.Neighbours.Count());
            Assert.AreEqual(0, roomB.Neighbours.Count());

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_NeighbourRooms_HaveSymmetricNeighbours()
        {
            var roomA = _plan.Add(new Vector2[] { new Vector2(-40, -40), new Vector2(-40, 0), new Vector2(-20, 0), new Vector2(-20, -40) }, 3f).Single();
            var roomB = _plan.Add(new Vector2[] { new Vector2(40, 20), new Vector2(40, -20), new Vector2(20, -20), new Vector2(20, 20) }, 3f).Single();

            _plan.Freeze();

            Assert.AreEqual(1, roomA.Neighbours.Count());
            Assert.AreEqual(1, roomB.Neighbours.Count());

            //Check that section is in right place
            var n = roomA.Neighbours.Single();
            Assert.IsTrue(Math.Abs(n.At - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 0.5f) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_StartOverlap_OccludesNeighbour()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f).Single();
            var roomMid = _plan.Add(new Vector2[] { new Vector2(20, 0), new Vector2(20, -40), new Vector2(-20, -40), new Vector2(-20, 0) }, 3f).Single();
            var roomRight = _plan.Add(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f).Single();

            _plan.Freeze();

            Assert.AreEqual(2, roomLeft.Neighbours.Count());
            Assert.AreEqual(2, roomRight.Neighbours.Count());
            Assert.AreEqual(2, roomMid.Neighbours.Count());

            //Check that section to mid room is in right place
            var n = roomLeft.Neighbours.Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 1) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 1) < 0.01f);

            //Check that section to right room does not overlap with mid room
            var m = roomLeft.Neighbours.Single(a => a.RoomCD == roomRight);
            Assert.IsTrue(Math.Abs(m.At - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Bt - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Ct - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Dt - 1) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_EndOverlap_OccludesNeighbour()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f).Single();
            var roomMid = _plan.Add(new Vector2[] { new Vector2(20, 40), new Vector2(20, 0), new Vector2(-20, 0), new Vector2(-20, 40) }, 3f).Single();
            var roomRight = _plan.Add(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f).Single();

            _plan.Freeze();

            Assert.AreEqual(2, roomLeft.Neighbours.Count());
            Assert.AreEqual(2, roomRight.Neighbours.Count());
            Assert.AreEqual(2, roomMid.Neighbours.Count());

            //Check that section to mid room is in right place
            var n = roomLeft.Neighbours.Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 0.5f) < 0.01f);

            //Check that section to right room does not overlap with mid room
            var m = roomLeft.Neighbours.Single(a => a.RoomCD == roomRight);
            Assert.IsTrue(Math.Abs(m.At - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Bt - 1) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Dt - 0.5f) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_TotalOverlap_OccludesNeighbour()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f).Single();
            var roomRight = _plan.Add(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f).Single();
            var roomMid = _plan.Add(new Vector2[] { new Vector2(20, 40), new Vector2(20, -40), new Vector2(-20, -40), new Vector2(-20, 40) }, 3f).Single();

            _plan.Freeze();

            Assert.AreEqual(1, roomLeft.Neighbours.Count());
            Assert.AreEqual(1, roomRight.Neighbours.Count());
            Assert.AreEqual(2, roomMid.Neighbours.Count());

            //Check that section to mid room is in right place
            var n = roomLeft.Neighbours.Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 1f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 0.75f) < 0.01f);

            //Check that left does not neighbour right
            Assert.IsFalse(roomLeft.Neighbours.Any(a => a.RoomCD == roomRight));

            //Check that section to right room does not overlap with mid room
            var m = roomRight.Neighbours.Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(m.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Bt - 1) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Ct - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Dt - 0.75f) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan, 1).ToString());
        }

        [TestMethod]
        public void Floorplan_MidOverlap_SplitsNeighbour()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f).Single();
            var roomRight = _plan.Add(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f).Single();
            var roomMid = _plan.Add(new Vector2[] { new Vector2(20, 10), new Vector2(20, -10), new Vector2(-20, -10), new Vector2(-20, 10) }, 3f).Single();

            _plan.Freeze();

            Assert.AreEqual(3, roomLeft.Neighbours.Count());
            Assert.AreEqual(3, roomRight.Neighbours.Count());
            Assert.AreEqual(2, roomMid.Neighbours.Count());

            //Check that section to mid room is in right place
            var n = roomLeft.Neighbours.Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 0.75f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 1) < 0.01f);

            //Check that section to right room does not overlap with mid room
            var m1 = roomLeft.Neighbours.First(a => a.RoomCD == roomRight);
            Assert.IsTrue(Math.Abs(m1.At - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Bt - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Ct - 0.75f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Dt - 1) < 0.01f);

            var m2 = roomLeft.Neighbours.Where(a => a.RoomCD == roomRight).Skip(1).First();
            Assert.IsTrue(Math.Abs(m2.At - 0.75f) < 0.01f);
            Assert.IsTrue(Math.Abs(m2.Bt - 1) < 0.01f);
            Assert.IsTrue(Math.Abs(m2.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(m2.Dt - 0.25f) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_CornerOverlap_GeneratesNoNeighbours()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(-20, -20), new Vector2(-20, 0), new Vector2(0, 0), new Vector2(0, -20) }, 5).Single();
            var roomRight = _plan.Add(new Vector2[] { new Vector2(5, -2), new Vector2(5, 20), new Vector2(25, 20), new Vector2(25, -2) }, 5).Single();

            Assert.IsFalse(roomLeft.GetWalls().Any(f => f.NeighbouringRoom != null));
            Assert.IsFalse(roomRight.GetWalls().Any(f => f.NeighbouringRoom != null));

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_ClippingRooms_GeneratesNonOverlappingRooms()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(-20, -20), new Vector2(-20, 0), new Vector2(0, 0), new Vector2(0, -20) }, 5).Single();
            var roomRight = _plan.Add(new Vector2[] { new Vector2(-5, -5), new Vector2(-5, 20), new Vector2(25, 20), new Vector2(25, -5) }, 5).Single();

            _plan.Freeze();

            Assert.IsNotNull(roomLeft);
            Assert.IsNotNull(roomRight);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_RoomOutsideFloor_GeneratesNoRoom()
        {
            var roomLeft = _plan.Add(new Vector2[] { new Vector2(200, -20), new Vector2(200, 0), new Vector2(220, 0), new Vector2(220, -20) }, 5).Any();

            Assert.IsFalse(roomLeft);
        }

        [TestMethod]
        public void Floorplan_RoomTotallyInsideOtherRoom_GeneratesNoRoom()
        {
            var roomBig = _plan.Add(new Vector2[] { new Vector2(-50, -50), new Vector2(-50, 50), new Vector2(50, 50), new Vector2(50, -50) }, 5).Single();
            var roomNone = _plan.Add(new Vector2[] { new Vector2(0, 0), new Vector2(0, 10), new Vector2(10, 10), new Vector2(10, 0) }, 5).Any();

            Assert.IsNotNull(roomBig);
            Assert.IsNotNull(roomNone);

            Assert.IsFalse(roomNone);
        }

        [TestMethod]
        public void SvgFloorplan()
        {
            var r = new Random(23523);

            const int floorCount = 1;
            for (int i = 0; i < floorCount; i++)
            {
                var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) }));

                for (int j = 0; j < 3; j++)
                {
                    var minX = r.Next(-25, 20);
                    var minY = r.Next(-25, 20);
                    var width = r.Next(10, 20);
                    var height = r.Next(10, 20);
                    plan.Add(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) },
                        r.Next(1, 5)
                    );
                }

                plan.Freeze();

                //Console.WriteLine(FloorplanToSvg(plan, 500, 0, 0, (i * floorHeight - floorCount * floorHeight)));
            }
        }

        [TestMethod]
        public void InnerWallTurn()
        {
            _plan.Add(new[]
            {
                new Vector2(-70, -75),
                new Vector2(0, -25),
                new Vector2(-50, -25),


                new Vector2(-50, 25),
                new Vector2(0, 25),
                new Vector2(0, 75),

                new Vector2(50, 75),
                new Vector2(50, -75),
            }, 5);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void ConcaveRoomNeighbours()
        {
            var a = _plan.Add(new[]
            {
                new Vector2(-100, 20),
                new Vector2(-100, 75),
                new Vector2(0, 75),
                new Vector2(0, 20),
            }, 5).Single();

            var b = _plan.Add(new[]
            {
                new Vector2(-100, -75),
                new Vector2(-100, -20),
                new Vector2(0, -20),
                new Vector2(0, -75),
            }, 5).Single();

            var c = _plan.Add(new[]
            {
                new Vector2(-50, -75),
                new Vector2(-50, 75),
                new Vector2(50, 75),
                new Vector2(50, -75),
            }, 5).Single();

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.IsNotNull(c);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void TheTest()
        {
            var b = _plan.Add(new[]
            {
                new Vector2(-99.9f, -50),
                new Vector2(-99.99f, -10),
                new Vector2(-30, -10),
                new Vector2(-30, -50),
            }, 5).Single();

            var c = _plan.Add(new[]
            {
                new Vector2(-69.9f, -50),
                new Vector2(-69.9f, 50),
                new Vector2(60, 50),
                new Vector2(60, -50),
            }, 5).Single();

            //B neighbours A, C, ~D
            var neighboursB = b.Neighbours;
            Assert.AreEqual(2, neighboursB.Count(x => x.Other(b) == c));

            //C neighbours A, B, D
            var neighboursC = c.Neighbours;
            Assert.AreEqual(2, neighboursC.Count(x => x.Other(c) == b));

            //Check that B has no duplicated facades
            var facadesB = b.GetWalls().ToArray();
            var duplicatesB = facadesB.Where(f => facadesB.Any(g => g != f && g.Section.Matches(f.Section))).ToArray();
            Assert.IsFalse(duplicatesB.Any());

            //Check that C has no duplicated facades
            var facadesC = c.GetWalls().ToArray();
            var duplicatesC = facadesC.Where(f => facadesC.Any(g => g != f && g.Section.Matches(f.Section))).ToArray();
            Assert.IsFalse(duplicatesC.Any());

            var svg = SvgRoomVisualiser.FloorplanToSvg(_plan);
            for (int i = 0; i < facadesC.Length; i++)
            {
                var s = facadesC[i].Section;
                var pos = s.A * 0.5f + s.B * 0.5f + s.C * 0.25f + s.D * 0.25f;
                svg.Descendants("g").Single().Add(
                    new XElement("text",
                        new XAttribute("x", pos.X),
                        new XAttribute("y", pos.Y),
                        new XAttribute("fill", "black"),
                        i.ToString(CultureInfo.InvariantCulture)
                    )
                );
            }

            Console.WriteLine(svg.ToString());
        }

        [TestMethod]
        public void TrainCarriageTest()
        {
            // ReSharper disable InconsistentNaming
            var HierarchicalParameters = new NamedBoxCollection();
            var r = new Random();
            Func<double> Random = r.NextDouble;

            const float Length = 60;
            const float Width = 20;

            Func<Vector2, float, float, Vector2> Offset = (start, length, width) => start + new Vector2(Length * length, -Width * width);

            Func<IFloorPlanBuilder, bool, float, IEnumerable<IRoomPlan>> CreateBalcony = (pl, start, bl) =>
            {
                var p = pl.ExternalFootprint.First();

                var wt = HierarchicalParameters.InternalWallThickness(Random);

                if (start)
                {
                    return pl.Add(new Vector2[]
                    {
                        Offset(p, 0, 0.01f),
                        Offset(p, bl / Length, 0.01f),
                        Offset(p, bl / Length, 0.99f),
                        Offset(p, 0, 0.99f),
                    }, wt);
                }
                else
                {
                    return pl.Add(new Vector2[]
                    {
                        Offset(p, 1 - (bl / Length), 0.01f),
                        Offset(p, 1, 0.01f),
                        Offset(p, 1, 0.99f),
                        Offset(p, 1 - bl / Length, 0.99f),
                    }, wt);
                }
            };

            var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(new Vector2[]
            {
                new Vector2(-Length / 2f, Width / 2f),
                new Vector2(Length / 2f, Width / 2f),
                new Vector2(Length / 2f, -Width / 2f),
                new Vector2(-Length / 2f, -Width / 2f),
            }));
// ReSharper restore InconsistentNaming

            //Get some style values
            var wallThickness =  HierarchicalParameters.InternalWallThickness(Random);
            var doorWidth = HierarchicalParameters.StandardDoorWidth(Random);

            //Create balconies on either end
            float balconyLength = Math.Min(3, Length / 10f);
            var balcony1 = CreateBalcony(plan, true, balconyLength).Single();
            var balcony2 = CreateBalcony(plan, false, balconyLength).Single();

            //Reference point to create rooms relative to
            var point = plan.ExternalFootprint.First();

            //Add toilets at one end of the carriage
            float toiletLength = balconyLength;

            //Left of the corridor
            var toiletLeft = plan.Add(new Vector2[]
            {
                Offset(point, balconyLength / Length, 0),
                Offset(point, (balconyLength + toiletLength) / Length, 0),
                Offset(point, (balconyLength + toiletLength) / Length, (Width / 2 - doorWidth / 2) / Width),
                Offset(point, balconyLength / Length, (Width / 2 - doorWidth / 2) / Width),
            }, wallThickness).Single();

            //Right of the corridor
            var toiletRight = plan.Add(new Vector2[]
            {
                Offset(point, balconyLength / Length, (Width / 2 + doorWidth / 2) / Width),
                Offset(point, (balconyLength + toiletLength) / Length, (Width / 2 + doorWidth / 2) / Width),
                Offset(point, (balconyLength + toiletLength) / Length, 1),
                Offset(point, balconyLength / Length, 1),
            }, wallThickness).Single();

            //Corridor
            var corridorL = (Width / 2 - doorWidth / 2 + 0.01f) / Width;
            var corridorR = (Width / 2 + doorWidth / 2 - 0.01f) / Width;
            var corridor = plan.Add(new Vector2[]
            {
                Offset(point, balconyLength / Length, corridorL),
                Offset(point, (balconyLength + toiletLength) / Length, corridorL),
                Offset(point, (balconyLength + toiletLength) / Length, corridorR),
                Offset(point, balconyLength / Length, corridorR),
            }, wallThickness).Single();

            //Add dining room
            var diningRoom = plan.Add(new Vector2[]
            {
                Offset(point, (balconyLength + toiletLength + 0.05f) / Length, 0),
                Offset(point, (Length - balconyLength - 0.05f) / Length, 0),
                Offset(point, (Length - balconyLength - 0.05f) / Length, 1),
                Offset(point, (balconyLength + toiletLength + 0.05f) / Length, 1),
            }, wallThickness).Single();

            Assert.IsFalse(balcony2.Neighbours.Any(a => a.Other(balcony2) != diningRoom));

            Assert.IsNotNull(balcony1);
            Assert.IsNotNull(balcony2);
            Assert.IsNotNull(toiletLeft);
            Assert.IsNotNull(toiletRight);
            Assert.IsNotNull(corridor);
            Assert.IsNotNull(diningRoom);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(plan).ToString());
        }

        [TestMethod]
        public void TrainCarriageTest2()
        {
            // ReSharper disable InconsistentNaming
            var r = new Random();
            Func<double> Random = r.NextDouble;

            const int Length = 20;
            const int Width = 10;

            Func<Vector2, float, float, Vector2> Offset = (start, length, width) => start + new Vector2(Length * length, -Width * width);

            Func<IFloorPlanBuilder, bool, float, IEnumerable<IRoomPlan>> CreateBalcony = (pl, start, bl) =>
            {
                var p = pl.ExternalFootprint.First();

                const float wt = 0.11f;

                if (start)
                {
                    return pl.Add(new Vector2[]
                    {
                        Offset(p, 0, 0.01f),
                        Offset(p, bl / Length, 0.01f),
                        Offset(p, bl / Length, 0.99f),
                        Offset(p, 0, 0.99f),
                    }, wt);
                }
                else
                {
                    return pl.Add(new Vector2[]
                    {
                        Offset(p, 1 - (bl / Length), 0.01f),
                        Offset(p, 1, 0.01f),
                        Offset(p, 1, 0.99f),
                        Offset(p, 1 - bl / Length, 0.99f),
                    }, wt);
                }
            };

            var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(new Vector2[]
            {
                new Vector2(-Length / 2f, Width / 2f),
                new Vector2(Length / 2f, Width / 2f),
                new Vector2(Length / 2f, -Width / 2f),
                new Vector2(-Length / 2f, -Width / 2f),
            }));
// ReSharper restore InconsistentNaming

            //Create balconies on either end
            float balconyLength = Math.Min(3, Length / 10f);
            CreateBalcony(plan, true, balconyLength);
            CreateBalcony(plan, false, balconyLength);

            //Reference point to create rooms relative to
            var point = plan.ExternalFootprint.First();

            //Create corridor section along entire train (along one side)
            const float corridorWidth = 3;
            plan.Add(new Vector2[]
            {
                Offset(point, (balconyLength + 0.05f) / Length, 0),
                Offset(point, (Length - balconyLength - 0.05f) / Length, 0),
                Offset(point, (Length - balconyLength - 0.05f) / Length, corridorWidth / Width),
                Offset(point, (balconyLength + 0.05f) / Length, corridorWidth / Width),
            }, 0.55f);

            //Create compartments
            var compartmentAreaLength = Length - (balconyLength + 0.05f) * 2;
            var compartmentCount = Random.CompartmentalizeSpace(compartmentAreaLength, 1, int.MaxValue, 6, 10);
            var compartmentLength = compartmentAreaLength / compartmentCount;

            var compartments = new IRoomPlan[compartmentCount];
            for (var i = 0; i < compartmentCount; i++)
            {
                var xStart = balconyLength + 0.05f + i * compartmentLength;
                var xEnd = xStart + compartmentLength - 0.05f;

                const float yStart = corridorWidth;

                compartments[i] = plan.Add(new Vector2[]
                {
                    Offset(point, xStart / Length, yStart / Width),
                    Offset(point, xEnd / Length, yStart / Width),
                    Offset(point, xEnd / Length, 1),
                    Offset(point, xStart / Length, 1),
                }, 0.55f).Single();
            }

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(plan, scalePosition: 10).ToString());
        }

        [TestMethod]
        public void FuzzTest()
        {
            Action<int, bool> iterate = (seed, catchit) =>
            {
                Random r = new Random(seed);

                try
                {
                    var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) }));

                    for (int j = 0; j < 3; j++)
                    {
                        var minX = r.Next(-25, 20);
                        var minY = r.Next(-25, 20);
                        var width = r.Next(10, 20);
                        var height = r.Next(10, 20);
                        plan.Add(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) }, 1);
                    }

                    plan.Freeze();

                    Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
                }
                catch
                {
                    if (!catchit)
                        throw;
                    else
                        Assert.Fail("Failing seed = " + seed);
                }
            };

            for (int s = 0; s < 100; s++)
            {
                iterate(s * 2389, true);
            }
        }

        [TestMethod]
        public void RegressionTest_ShrinkingSplitsFootprint()
        {
            // This is a case generated from fuzz testing (i.e. generate random data, see what breaks).
            // Shrinking a shape can generate *several* separate shapes if the original shape was convex.
            // This used to fail, now shrinking discards all the generated shapes except the largest (fixed with a change in EpimetheusPlugins).

            var r = new Random(738);

            var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) }));

            for (int j = 0; j < 3; j++)
            {
                var minX = r.Next(-25, 20);
                var minY = r.Next(-25, 20);
                var width = r.Next(10, 20);
                var height = r.Next(10, 20);
                plan.Add(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) },
                    1
                );
            }

            plan.Freeze();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void RegressionTest_OppositeWallSectionsAreNotDuplicated()
        {
            // This is a case I found whilst designing trains
            // This particular setup resulted in the right hand room (d) having *two* left walls.
            // One wall was just a wall (no neighbours) and another wall was a neighbour for the big wall in the middle (they overlapped)

            var a = _plan.Add(new[]
            {
                new Vector2(-100, -50),
                new Vector2(-100, 50),
                new Vector2(-70, 50),
                new Vector2(-70, -50),
            }, 5).Single();

            var b = _plan.Add(new[]
            {
                new Vector2(-70, -50),
                new Vector2(-70, -10),
                new Vector2(-30, -10),
                new Vector2(-30, -50),
            }, 5).Single();

            var b2 = _plan.Add(new[]
            {
                new Vector2(-30, 50),
                new Vector2(-30, 10),
                new Vector2(-70, 10),
                new Vector2(-70, 50),
            }, 5).Single();

            var c = _plan.Add(new[]
            {
                new Vector2(-69.9f, -50),
                new Vector2(-69.9f, 50),
                new Vector2(60, 50),
                new Vector2(60, -50),
            }, 5).Single();

            var d = _plan.Add(new[]
            {
                new Vector2(70, -50),
                new Vector2(70, 50),
                new Vector2(100, 50),
                new Vector2(100, -50),
            }, 5).Single();

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.IsNotNull(b2);
            Assert.IsNotNull(c);
            Assert.IsNotNull(d);

            //A neighbours B, C, ~D
            Assert.AreEqual(1, a.Neighbours.Count(x => x.Other(a) == b));
            Assert.AreEqual(1, a.Neighbours.Count(x => x.Other(a) == c));
            Assert.AreEqual(0, a.Neighbours.Count(x => x.Other(a) == d));

            //B neighbours A, C, ~D
            Assert.AreEqual(1, b.Neighbours.Count(x => x.Other(b) == a));
            Assert.AreEqual(2, b.Neighbours.Count(x => x.Other(b) == c));
            Assert.AreEqual(0, b.Neighbours.Count(x => x.Other(b) == d));

            //C neighbours A, B, D
            Assert.AreEqual(1, c.Neighbours.Count(x => x.Other(c) == a));
            Assert.AreEqual(2, c.Neighbours.Count(x => x.Other(c) == b));
            Assert.AreEqual(1, c.Neighbours.Count(x => x.Other(c) == d));

            //D neighbours ~A, ~B, C
            Assert.AreEqual(0, d.Neighbours.Count(x => x.Other(d) == a));
            Assert.AreEqual(0, d.Neighbours.Count(x => x.Other(d) == b));
            Assert.AreEqual(1, d.Neighbours.Count(x => x.Other(d) == c));

            var duplicateCheck = c;
            var facades = duplicateCheck.GetWalls().ToArray();
            var duplicates = facades.Where(f => facades.Any(g => g != f && g.Section.Matches(f.Section))).ToArray();
            Assert.IsFalse(duplicates.Any());

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void RegressionTest_MissingFacadeStartSections()
        {
            // This is a test case found when designing trains
            // The start of a wall (from wall start -> start of first neighbour) didn't generate, now it does.
            // This test will fail if that happens again

            var b = _plan.Add(new[]
            {
                new Vector2(-70, -50),
                new Vector2(-70, -10),
                new Vector2(-30, -10),
                new Vector2(-30, -50),
            }, 5).Single();

            var c = _plan.Add(new[]
            {
                new Vector2(-69.9f, -50),
                new Vector2(-69.9f, 50),
                new Vector2(60, 50),
                new Vector2(60, -50),
            }, 5).Single();

            Assert.AreEqual(8, b.GetWalls().Count());
            Assert.AreEqual(14, c.GetWalls().Count());

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void RegressionTest_UnmatchedWallSections()
        {
            // This is a case generated from fuzz testing (i.e. generate random data, see what breaks).
            // Sometimes matching up inner and outer sections of wall data used to fail (fixed in EpimetheusPlugins).
            // This test will fail in that case

            var r = new Random(189);

            var plan = new GeometricFloorplan(new ReadOnlyCollection<Vector2>(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) }));

            for (int j = 0; j < 3; j++)
            {
                var minX = r.Next(-25, 20);
                var minY = r.Next(-25, 20);
                var width = r.Next(10, 20);
                var height = r.Next(10, 20);
                plan.Add(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) },
                    1
                );
            }

            plan.Freeze();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void RegressionTest_NaN_WallSections()
        {
            // This is a case generated from fuzz testing (i.e. generate random data, see what breaks).
            // This room is shaped like:
            //
            // +---------+
            // |         |
            // X---X     |
            //     |     |
            //     +-----+
            //
            // Shrinking this room results in a corner like this (at the X--X edge):
            //
            // |    +-----+
            // |          |
            // X----X     |
            //
            // The inside point is aligned with the outside point, logically this wall section is just two corners with no facade in the center.
            // Before fixing this case, some NaN sections were generated because of this case, now they aren't, and this test makes sure we don't undo that change.

            var v = new[] { new Vector2(15, 14), new Vector2(2, 14), new Vector2(2, 22), new Vector2(1, 22), new Vector2(1, 25), new Vector2(15, 25) };

            var r = _plan.Add(v, 1);

            _plan.Freeze();

            var f = r.Single().GetWalls();

            foreach (var facade in f)
            {
                Assert.IsFalse(facade.Section.A.IsNaN());
                Assert.IsFalse(facade.Section.B.IsNaN());
                Assert.IsFalse(facade.Section.C.IsNaN());
                Assert.IsFalse(facade.Section.D.IsNaN());
                Assert.IsFalse(facade.Section.Along.IsNaN());
            }
        }
    }
}
