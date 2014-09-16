using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Styles;
using Base_CityGeneration.TestHelpers;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Myre.Collections;
using Myre.Extensions;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors
{
    [TestClass]
    public class FloorPlanTest
    {
        private FloorPlan _plan;

        [TestInitialize]
        public void Initialize()
        {
            _plan = new FloorPlan(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(100, 100), new Vector2(100, -100) });
        }

        [TestMethod]
        public void RoomInternalBordersAreSmaller()
        {
            var r = _plan.AddRoom(new Vector2[]
            {
                new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10)
            }, 0.1f, new ScriptReference[0], false).Single();

            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(-9.9f, 9.9f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(-9.9f, -9.9f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(9.9f, 9.9f), 0.1f));
            Assert.IsTrue(r.InnerFootprint.RoughlyContains(new Vector2(9.9f, -9.9f), 0.1f));
        }

        [TestMethod]
        public void RoomInternalBordersAreSmallerWhenNotAtOrigin()
        {
            var r = _plan.AddRoom(new Vector2[]
            {
                new Vector2(10, 10), new Vector2(10, 30), new Vector2(30, 30), new Vector2(30, 10)
            }, 0.1f, new ScriptReference[0], false).Single();

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
            _plan.AddRoom(new Vector2[0], 0.1f, new ScriptReference[0], false);
        }

        [TestMethod]
        public void AddNewRoomToEmptyFloorPlanSucceeds()
        {
            Assert.IsTrue(_plan.AddRoom(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f,
                new ScriptReference[0]).Any()
            );
        }

        [TestMethod]
        public void AddNewSplitRoomsFails()
        {
            Assert.IsTrue(_plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]).Any()
            );

            Assert.IsFalse(_plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]).Any()
            );
        }

        [TestMethod]
        public void AddNewSplitRoomsSucceeds()
        {
            Assert.IsTrue(_plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]).Any()
            );

            Assert.IsTrue(_plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0],
                true).Any()
            );
        }

        [TestMethod]
        public void AddNewRoomOutsideFloorBoundsIsClipped()
        {
            Assert.IsTrue(_plan.AddRoom(
                new Vector2[] { new Vector2(-200, -10), new Vector2(-200, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]).Any()
            );
        }

        [TestMethod]
        public void ExactlyMirroredRoomsAreNeighbours()
        {
            var a = _plan.AddRoom(new Vector2[] {new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(0, 10), new Vector2(0, -10)}, 1, new ScriptReference[0], false).Single();
            var b = _plan.AddRoom(new Vector2[] {new Vector2(0, -10), new Vector2(0, 10), new Vector2(10, 10), new Vector2(10, -10)}, 1, new ScriptReference[0], false).Single();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());

            Assert.IsTrue(_plan.GetNeighbours(a).Any(n => n.Other(a) == b));
        }

        [TestMethod]
        public void NeighboursOfSingleRoomAreNone()
        {
            var r = _plan.AddRoom(
                new Vector2[] { new Vector2(-200, -10), new Vector2(-200, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]).Single();

            var n = _plan.GetNeighbours(r);
            Assert.IsFalse(n.Any());
        }

        [TestMethod]
        [Timeout(1000)]
        public void GetRoomNeighboursFindsBasicPair()
        {
            //A really wide room
            RoomPlan wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //Low room
            RoomPlan low = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide);
            Assert.AreEqual(1, wideNeighbours.Count());

            //Check that points C and D lies on the edge of low room
            var n = wideNeighbours.Single(a => a.RoomCD == low);
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.C, new Line2D(new Vector2(-10, -90f), new Vector2(1, 0))) < 0.1f);
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.D, new Line2D(new Vector2(-10, -90f), new Vector2(1, 0))) < 0.1f);

            //Check that points A and B lie on the edge of wide room
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.A, new Line2D(new Vector2(-10, -10f), new Vector2(1, 0))) < 0.1f);
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.B, new Line2D(new Vector2(-10, -10f), new Vector2(1, 0))) < 0.1f);

            //Check that neighbour data is the same going the other direction
            var lowNeighbours = _plan.GetNeighbours(low);
            Assert.AreEqual(1, lowNeighbours.Count());
            Assert.IsTrue(lowNeighbours.Any(a => a.RoomCD == wide));

            //Check that point is close to the edge it is supposed to lie on
            var segment = new LineSegment2D(wide.OuterFootprint[n.EdgeIndexRoomAB], wide.OuterFootprint[(n.EdgeIndexRoomAB + 1) % wide.OuterFootprint.Length]);
            var line = segment.Line();
            var dist = Geometry2D.DistanceFromPointToLine(n.A, line);
            Assert.IsTrue(dist < 0.01f);

            //Check that point is close to the edge it is supposed to lie on
            var segment2 = new LineSegment2D(low.OuterFootprint[n.EdgeIndexRoomCD], low.OuterFootprint[(n.EdgeIndexRoomCD + 1) % low.OuterFootprint.Length]);
            var line2 = segment2.Line();
            var dist2 = Geometry2D.DistanceFromPointToLine(n.C, line2);
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
            RoomPlan wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            RoomPlan high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide).Single();

            Assert.IsTrue(new[] { wideNeighbours.A, wideNeighbours.B, wideNeighbours.C, wideNeighbours.D }.Area() < 0);
        }

        [TestMethod]
        [Timeout(1000)]
        public void GetRoomNeighboursFindsBasicPairReversed()
        {
            //A really wide room
            RoomPlan wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            RoomPlan high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide);
            Assert.AreEqual(1, wideNeighbours.Count());
            Assert.IsTrue(wideNeighbours.Any(a => a.RoomCD == high));

            var lowNeighbours = _plan.GetNeighbours(high);
            Assert.AreEqual(1, lowNeighbours.Count());
            Assert.IsTrue(lowNeighbours.Any(a => a.RoomCD == wide));
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


            RoomPlan a = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            RoomPlan b = _plan.AddRoom(
                new Vector2[] { new Vector2(-15, -30), new Vector2(-15, -20), new Vector2(0, -20), new Vector2(0, -30) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var aNeighbours = _plan.GetNeighbours(a);
            Assert.AreEqual(1, aNeighbours.Count());
            var n1 = aNeighbours.Single(x => x.RoomCD == b);
            var n2 = aNeighbours.Single(x => x.RoomAB == a);

            Assert.AreEqual(n1, n2);

            Assert.IsTrue(a.OuterFootprint.Where(p => Vector2.Distance(p, n1.B) < 0.1f).Any());
            Assert.IsTrue(b.OuterFootprint.Where(p => Vector2.Distance(p, n1.D) < 0.1f).Any());
        }

        [TestMethod]
        public void DisjointRoomsAreHandled()
        {
            //A really wide room
            RoomPlan a = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            RoomPlan b = _plan.AddRoom(
                new Vector2[] { new Vector2(15, -30), new Vector2(15, -20), new Vector2(20, -20), new Vector2(20, -30) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var aNeighbours = _plan.GetNeighbours(a);
            Assert.AreEqual(0, aNeighbours.Count());
        }

        [TestMethod]
        public void RoomsOccludeFartherRoomsFromBeingNeighbours()
        {
            //Low room
            RoomPlan low = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            RoomPlan high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //A really wide room (which should occlude low and high from being neighbours)
            RoomPlan wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide);
            Assert.AreEqual(2, wideNeighbours.Count());
            Assert.IsTrue(wideNeighbours.Any(a => a.RoomCD == low));
            Assert.IsTrue(wideNeighbours.Any(a => a.RoomCD == high));

            var lowNeighbours = _plan.GetNeighbours(low);
            Assert.AreEqual(1, lowNeighbours.Count());
            Assert.IsTrue(lowNeighbours.Any(a => a.RoomCD == wide));

            var highNeighbours = _plan.GetNeighbours(high);
            Assert.AreEqual(1, highNeighbours.Count());
            Assert.IsTrue(highNeighbours.Any(a => a.RoomCD == wide));
        }

        [TestMethod]
        public void GetRoomNeighboursFindsNeighbours()
        {
            //A really wide room
            RoomPlan wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //Low room
            RoomPlan low = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            RoomPlan high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High left
            RoomPlan highLeft = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, 100), new Vector2(-90, 100), new Vector2(-90, 90), new Vector2(-100, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide);
            Assert.AreEqual(3, wideNeighbours.Count());
            Assert.IsTrue(wideNeighbours.Any(a => a.RoomCD == low));
            Assert.IsTrue(wideNeighbours.Any(a => a.RoomCD == high));
            Assert.IsTrue(wideNeighbours.Any(a => a.RoomCD == highLeft));

            var lowNeighbours = _plan.GetNeighbours(low);
            Assert.AreEqual(1, lowNeighbours.Count());
            Assert.IsTrue(lowNeighbours.Any(a => a.RoomCD == wide));

            var highNeighbours = _plan.GetNeighbours(high);
            Assert.AreEqual(2, highNeighbours.Count());
            Assert.IsTrue(highNeighbours.Any(a => a.RoomCD == wide));
            Assert.IsTrue(highNeighbours.Any(a => a.RoomCD == highLeft));
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesExternalSections()
        {
            var room = _plan.AddRoom(_plan.ExternalFootprint, 0.25f, new ScriptReference[0]).Single();

            _plan.Freeze();

            var facades = room.GetFacades().ToArray();

            Assert.AreEqual(8, facades.Count());
            Assert.AreEqual(4, facades.Where(f => f.Section.IsCorner).Count());
            Assert.AreEqual(4, facades.Where(f => !f.Section.IsCorner).Count());
            Assert.IsTrue(facades.All(f => f.IsExternal));
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesExternalSectionsForHalfExternalRoom()
        {
            var room = _plan.AddRoom(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 0.25f, new ScriptReference[0]).Single();

            _plan.Freeze();

            var facades = room.GetFacades().ToArray();

            Assert.AreEqual(4, facades.Where(f => f.Section.IsCorner).Count());
            Assert.AreEqual(7, facades.Where(f => f.IsExternal).Count());
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesInternalSections()
        {
            var room = _plan.AddRoom(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 0.25f, new ScriptReference[0]).Single();

            _plan.Freeze();

            var facades = room.GetFacades().ToArray();

            Assert.AreEqual(4, facades.Where(f => !f.Section.IsCorner).Count());
            Assert.AreEqual(1, facades.Where(f => !f.IsExternal).Count());
        }

        [TestMethod]
        public void RoomSectionGenerationGeneratesNeighbourSections()
        {
            var roomA = _plan.AddRoom(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 3.25f, new ScriptReference[0]).Single();
            var roomB = _plan.AddRoom(new Vector2[] { new Vector2(10, -10), new Vector2(10, 10), new Vector2(20, 10), new Vector2(20, -10) }, 3.25f, new ScriptReference[0]).Single();

            _plan.Freeze();

            var facades = roomA.GetFacades().ToArray();

            var internalFacades = facades.Where(f => !f.IsExternal).ToArray();

            Assert.AreEqual(1, internalFacades.Where(f => f.NeighbouringRoom == roomB).Count());
            Assert.AreEqual(3, internalFacades.Count());

            //Check that the neighbour section has points lying on both rooms
            var n = internalFacades.Where(f => f.NeighbouringRoom == roomB).Single();
            //Assert.IsTrue(roomB.InnerFootprint.Where(p => Vector2.Distance(p, n.Section.C) < 0.1f).Any());
            //Assert.IsTrue(roomA.InnerFootprint.Where(p => Vector2.Distance(p, n.Section.A) < 0.1f).Any());

            ////Check that section has points adjacent to right edge of room A
            //var segment = new LineSegment2D(roomA.OuterFootprint[3], roomA.OuterFootprint[0]);
            //var line = segment.Line();
            //var dist = Geometry2D.DistanceFromPointToLine(n.Section.D, line);
            //Assert.IsTrue(dist < 0.01f);

            ////Check that section has points adjacent to left edge of room B
            //var segment2 = new LineSegment2D(roomB.OuterFootprint[1], roomB.OuterFootprint[2]);
            //var line2 = segment2.Line();
            //var dist2 = Geometry2D.DistanceFromPointToLine(n.Section.C, line2);
            //Assert.IsTrue(dist2 < 0.01f);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        private void AssertAllWindings()
        {
            foreach (var roomInfo in _plan.Rooms)
            {
                foreach (var neighbour in _plan.GetNeighbours(roomInfo))
                {
                    Assert.IsTrue(new Vector2[] {neighbour.A, neighbour.B, neighbour.C, neighbour.D}.Area() < 0);
                }
            }
        }

        private void AssertAllSections()
        {
            Func<RoomPlan, LineSegment2D[]> edges = r => r.OuterFootprint.Select((a, i) => new LineSegment2D(a, r.OuterFootprint[(i + 1) % r.OuterFootprint.Length])).ToArray();

            foreach (var neighbour in _plan.Rooms.SelectMany(roomInfo => _plan.GetNeighbours(roomInfo)))
            {
                Assert.IsTrue(edges(neighbour.RoomAB).Any(e => Geometry2D.DistanceFromPointToLineSegment(neighbour.A, e) < 0.1f));
                Assert.IsTrue(edges(neighbour.RoomAB).Any(e => Geometry2D.DistanceFromPointToLineSegment(neighbour.B, e) < 0.1f));
                Assert.IsTrue(edges(neighbour.RoomCD).Any(e => Geometry2D.DistanceFromPointToLineSegment(neighbour.C, e) < 0.1f));
                Assert.IsTrue(edges(neighbour.RoomCD).Any(e => Geometry2D.DistanceFromPointToLineSegment(neighbour.D, e) < 0.1f));
            }
        }

        [TestMethod]
        public void Floorplan_SeparateRooms_HaveNoNeighbours()
        {
            var roomA = _plan.AddRoom(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, -80), new Vector2(-80, -80), new Vector2(-80, -100) }, 3f, new ScriptReference[0]).Single();
            var roomB = _plan.AddRoom(new Vector2[] { new Vector2(100, 100), new Vector2(100, 80), new Vector2(80, 80), new Vector2(80, 100) }, 3f, new ScriptReference[0]).Single();

            _plan.Freeze();

            Assert.AreEqual(0, _plan.GetNeighbours(roomA).Count());
            Assert.AreEqual(0, _plan.GetNeighbours(roomB).Count());

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_NeighbourRooms_HaveSymmetricNeighbours()
        {
            var roomA = _plan.AddRoom(new Vector2[] { new Vector2(-40, -40), new Vector2(-40, 0), new Vector2(-20, 0), new Vector2(-20, -40) }, 3f, new ScriptReference[0]).Single();
            var roomB = _plan.AddRoom(new Vector2[] { new Vector2(40, 20), new Vector2(40, -20), new Vector2(20, -20), new Vector2(20, 20) }, 3f, new ScriptReference[0]).Single();

            _plan.Freeze();

            Assert.AreEqual(1, _plan.GetNeighbours(roomA).Count());
            Assert.AreEqual(1, _plan.GetNeighbours(roomB).Count());

            //Check that section is in right place
            var n = _plan.GetNeighbours(roomA).Single();
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
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f, new ScriptReference[0]).Single();
            var roomMid = _plan.AddRoom(new Vector2[] { new Vector2(20, 0), new Vector2(20, -40), new Vector2(-20, -40), new Vector2(-20, 0) }, 3f, new ScriptReference[0]).Single();
            var roomRight = _plan.AddRoom(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f, new ScriptReference[0]).Single();

            _plan.Freeze();

            Assert.AreEqual(2, _plan.GetNeighbours(roomLeft).Count());
            Assert.AreEqual(2, _plan.GetNeighbours(roomRight).Count());
            Assert.AreEqual(2, _plan.GetNeighbours(roomMid).Count());

            //Check that section to mid room is in right place
            var n = _plan.GetNeighbours(roomLeft).Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 1) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 1) < 0.01f);

            //Check that section to right room does not overlap with mid room
            var m = _plan.GetNeighbours(roomLeft).Single(a => a.RoomCD == roomRight);
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
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f, new ScriptReference[0]).Single();
            var roomMid = _plan.AddRoom(new Vector2[] { new Vector2(20, 40), new Vector2(20, 0), new Vector2(-20, 0), new Vector2(-20, 40) }, 3f, new ScriptReference[0]).Single();
            var roomRight = _plan.AddRoom(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f, new ScriptReference[0]).Single();

            _plan.Freeze();

            Assert.AreEqual(2, _plan.GetNeighbours(roomLeft).Count());
            Assert.AreEqual(2, _plan.GetNeighbours(roomRight).Count());
            Assert.AreEqual(2, _plan.GetNeighbours(roomMid).Count());

            //Check that section to mid room is in right place
            var n = _plan.GetNeighbours(roomLeft).Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 0.5f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 0.5f) < 0.01f);

            //Check that section to right room does not overlap with mid room
            var m = _plan.GetNeighbours(roomLeft).Single(a => a.RoomCD == roomRight);
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
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f, new ScriptReference[0]).Single();
            var roomRight = _plan.AddRoom(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f, new ScriptReference[0]).Single();
            var roomMid = _plan.AddRoom(new Vector2[] { new Vector2(20, 40), new Vector2(20, -40), new Vector2(-20, -40), new Vector2(-20, 40) }, 3f, new ScriptReference[0]).Single();

            _plan.Freeze();

            Assert.AreEqual(1, _plan.GetNeighbours(roomLeft).Count());
            Assert.AreEqual(1, _plan.GetNeighbours(roomRight).Count());
            Assert.AreEqual(2, _plan.GetNeighbours(roomMid).Count());

            //Check that section to mid room is in right place
            var n = _plan.GetNeighbours(roomLeft).Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 1f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 0.75f) < 0.01f);

            //Check that left does not neighbour right
            Assert.IsFalse(_plan.GetNeighbours(roomLeft).Any(a => a.RoomCD == roomRight));

            //Check that section to right room does not overlap with mid room
            var m = _plan.GetNeighbours(roomRight).Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(m.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Bt - 1) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Ct - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(m.Dt - 0.75f) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_MidOverlap_SplitsNeighbour()
        {
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(-100, -20), new Vector2(-100, 20), new Vector2(-80, 20), new Vector2(-80, -20) }, 3f, new ScriptReference[0]).Single();
            var roomRight = _plan.AddRoom(new Vector2[] { new Vector2(100, 20), new Vector2(100, -20), new Vector2(80, -20), new Vector2(80, 20) }, 3f, new ScriptReference[0]).Single();
            var roomMid = _plan.AddRoom(new Vector2[] { new Vector2(20, 10), new Vector2(20, -10), new Vector2(-20, -10), new Vector2(-20, 10) }, 3f, new ScriptReference[0]).Single();

            _plan.Freeze();

            Assert.AreEqual(3, _plan.GetNeighbours(roomLeft).Count());
            Assert.AreEqual(3, _plan.GetNeighbours(roomRight).Count());
            Assert.AreEqual(2, _plan.GetNeighbours(roomMid).Count());

            //Check that section to mid room is in right place
            var n = _plan.GetNeighbours(roomLeft).Single(a => a.RoomCD == roomMid);
            Assert.IsTrue(Math.Abs(n.At - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Bt - 0.75f) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Ct - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(n.Dt - 1) < 0.01f);

            //Check that section to right room does not overlap with mid room
            var m1 = _plan.GetNeighbours(roomLeft).First(a => a.RoomCD == roomRight);
            Assert.IsTrue(Math.Abs(m1.At - 0) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Bt - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Ct - 0.75f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Dt - 1) < 0.01f);

            var m2 = _plan.GetNeighbours(roomLeft).Where(a => a.RoomCD == roomRight).Skip(1).First();
            Assert.IsTrue(Math.Abs(m1.At - 0f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Bt - 0.25f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Ct - 0.75f) < 0.01f);
            Assert.IsTrue(Math.Abs(m1.Dt - 1f) < 0.01f);

            //Check all neighbour data is correctly wound
            AssertAllWindings();

            //Check all sections lies on the external footprint of the involved rooms
            AssertAllSections();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_CornerOverlap_GeneratesNoNeighbours()
        {
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(-20, -20), new Vector2(-20, 0), new Vector2(0, 0), new Vector2(0, -20) }, 5, new ScriptReference[0]).Single();
            var roomRight = _plan.AddRoom(new Vector2[] { new Vector2(5, -2), new Vector2(5, 20), new Vector2(25, 20), new Vector2(25, -2) }, 5, new ScriptReference[0]).Single();

            Assert.IsFalse(roomLeft.GetFacades().Any(f => f.NeighbouringRoom != null));
            Assert.IsFalse(roomRight.GetFacades().Any(f => f.NeighbouringRoom != null));

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_ClippingRooms_GeneratesNonOverlappingRooms()
        {
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(-20, -20), new Vector2(-20, 0), new Vector2(0, 0), new Vector2(0, -20) }, 5, new ScriptReference[0]).Single();
            var roomRight = _plan.AddRoom(new Vector2[] { new Vector2(-5, -5), new Vector2(-5, 20), new Vector2(25, 20), new Vector2(25, -5) }, 5, new ScriptReference[0]).Single();

            _plan.Freeze();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void Floorplan_RoomOutsideFloor_GeneratesNoRoom()
        {
            var roomLeft = _plan.AddRoom(new Vector2[] { new Vector2(200, -20), new Vector2(200, 0), new Vector2(220, 0), new Vector2(220, -20) }, 5, new ScriptReference[0]).Any();

            Assert.IsFalse(roomLeft);
        }

        [TestMethod]
        public void Floorplan_RoomTotallyInsideOtherRoom_GeneratesNoRoom()
        {
            var roomBig = _plan.AddRoom(new Vector2[] { new Vector2(-50, -50), new Vector2(-50, 50), new Vector2(50, 50), new Vector2(50, -50) }, 5, new ScriptReference[0]).Single();
            var roomNone = _plan.AddRoom(new Vector2[] { new Vector2(0, 0), new Vector2(0, 10), new Vector2(10, 10), new Vector2(10, 0) }, 5, new ScriptReference[0]).Any();

            Assert.IsFalse(roomNone);
        }

        [TestMethod]
        public void SvgFloorplan()
        {
            Random r = new Random(23523);

            const int floorCount = 1;
            const int floorHeight = 20;
            for (int i = 0; i < floorCount; i++)
            {
                FloorPlan plan = new FloorPlan(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) });

                for (int j = 0; j < 3; j++)
                {
                    var minX = r.Next(-25, 20);
                    var minY = r.Next(-25, 20);
                    var width = r.Next(10, 20);
                    var height = r.Next(10, 20);
                    plan.AddRoom(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) },
                        r.Next(1, 5),
                        new ScriptReference[0]
                        );
                }

                plan.Freeze();

                //Console.WriteLine(FloorplanToSvg(plan, 500, 0, 0, (i * floorHeight - floorCount * floorHeight)));
            }
        }

        [TestMethod]
        public void InnerWallTurn()
        {
            _plan.AddRoom(new[]
            {
                new Vector2(-70, -75),
                new Vector2(0, -25),
                new Vector2(-50, -25),


                new Vector2(-50, 25),
                new Vector2(0, 25),
                new Vector2(0, 75),

                new Vector2(50, 75),
                new Vector2(50, -75),
            }, 5, new ScriptReference[0], false);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void ConcaveRoomNeighbours()
        {
            var a = _plan.AddRoom(new[]
            {
                new Vector2(-100, 20),
                new Vector2(-100, 75),
                new Vector2(0, 75),
                new Vector2(0, 20),
            }, 5, new ScriptReference[0], false).Single();

            var b = _plan.AddRoom(new[]
            {
                new Vector2(-100, -75),
                new Vector2(-100, -20),
                new Vector2(0, -20),
                new Vector2(0, -75),
            }, 5, new ScriptReference[0], false).Single();

            var c = _plan.AddRoom(new[]
            {
                new Vector2(-50, -75),
                new Vector2(-50, 75),
                new Vector2(50, 75),
                new Vector2(50, -75),
            }, 5, new ScriptReference[0], false).Single();

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void TheTest()
        {
            var b = _plan.AddRoom(new[]
            {
                new Vector2(-99.9f, -50),
                new Vector2(-99.99f, -10),
                new Vector2(-30, -10),
                new Vector2(-30, -50),
            }, 5, new ScriptReference[0], false).Single();

            var c = _plan.AddRoom(new[]
            {
                new Vector2(-69.9f, -50),
                new Vector2(-69.9f, 50),
                new Vector2(60, 50),
                new Vector2(60, -50),
            }, 5, new ScriptReference[0], true).Single();

            //B neighbours A, C, ~D
            var neighboursB = _plan.GetNeighbours(b);
            Assert.AreEqual(2, neighboursB.Count(x => x.Other(b) == c));

            //C neighbours A, B, D
            var neighboursC = _plan.GetNeighbours(c);
            Assert.AreEqual(2, neighboursC.Count(x => x.Other(c) == b));

            //Check that B has no duplicated facades
            var facadesB = b.GetFacades().ToArray();
            var duplicatesB = facadesB.Where(f => facadesB.Any(g => g != f && g.Section.Matches(f.Section))).ToArray();
            Assert.IsFalse(duplicatesB.Any());

            //Check that C has no duplicated facades
            var facadesC = c.GetFacades().ToArray();
            var duplicatesC = facadesC.Where(f => facadesC.Any(g => g != f && g.Section.Matches(f.Section))).ToArray();
            Assert.IsFalse(duplicatesC.Any());

            var count = c.GetFacades().Count();

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

        //[TestMethod]
        //public void ZClipperTest()
        //{
        //    Clipper clipper = new Clipper();

        //    Func<Vector2, IntPoint> toPoint = a => new IntPoint((long)(a.X * 10000), (long)a.Y * 10000);

        //    clipper.Clear();
        //    clipper.AddPolygon(new[] { new Vector2(0, 0), new Vector2(0, 100), new Vector2(100,100), new Vector2(100, 0) }.Select(toPoint).ToList(), PolyType.Subject);
        //    //clipper.AddPolygon(new[] { new Vector2(0, 0), new Vector2(0, 50), new Vector2(50, 50), new Vector2(50, 0) }.Reverse().Select(toPoint).ToList(), PolyType.Clip);
        //    clipper.AddPolygon(new[] { new Vector2(60, 60), new Vector2(60, 90), new Vector2(60, 90), new Vector2(60, 60) }.Reverse().Select(toPoint).ToList(), PolyType.Clip);

        //    //var solution = new List<List<IntPoint>>();
        //    var solution = new PolyTree();
        //    clipper.Execute(ClipType.Difference, solution);

        //    //solution.
        //    //Assert.AreEqual(1, solution.Count);
        //}

        [TestMethod]
        public void TrainCarriageTest()
        {
            // ReSharper disable InconsistentNaming
            var HierarchicalParameters = new NamedBoxCollection();
            var r = new Random();
            Func<double> Random = r.NextDouble;

            var Length = 60;
            var Width = 20;

            Func<Vector2, float, float, Vector2> Offset = (start, length, width) => start + new Vector2(Length * length, -Width * width);

            Func<FloorPlan, bool, float, IEnumerable<RoomPlan>> CreateBalcony = (pl, start, bl) =>
            {
                var p = pl.ExternalFootprint.First();

                var wt = HierarchicalParameters.InternalWallThickness(Random);

                if (start)
                {
                    return pl.AddRoom(new Vector2[]
                    {
                        Offset(p, 0, 0.01f),
                        Offset(p, bl / Length, 0.01f),
                        Offset(p, bl / Length, 0.99f),
                        Offset(p, 0, 0.99f),
                    }, wt, new ScriptReference[0]);
                }
                else
                {
                    return pl.AddRoom(new Vector2[]
                    {
                        Offset(p, 1 - (bl / Length), 0.01f),
                        Offset(p, 1, 0.01f),
                        Offset(p, 1, 0.99f),
                        Offset(p, 1 - bl / Length, 0.99f),
                    }, wt, new ScriptReference[0]);
                }
            };

            var plan = new FloorPlan(new Vector2[]
            {
                new Vector2(-Length / 2f, Width / 2f),
                new Vector2(Length / 2f, Width / 2f),
                new Vector2(Length / 2f, -Width / 2f),
                new Vector2(-Length / 2f, -Width / 2f),
            });
// ReSharper restore InconsistentNaming

            //Get some style values
            var wallThickness =  HierarchicalParameters.InternalWallThickness(Random);
            var doorWidth = HierarchicalParameters.StandardDoorWidth(Random);

            //Create balconies on either end
            float balconyLength = Math.Min(3, Length / 10f);
            var _balcony1 = CreateBalcony(plan, true, balconyLength).Single();
            var _balcony2 = CreateBalcony(plan, false, balconyLength).Single();

            //Reference point to create rooms relative to
            var point = plan.ExternalFootprint.First();

            //Add toilets at one end of the carriage
            float toiletLength = balconyLength;

            //Left of the corridor
            var _toiletLeft = plan.AddRoom(new Vector2[]
            {
                Offset(point, balconyLength / Length, 0),
                Offset(point, (balconyLength + toiletLength) / Length, 0),
                Offset(point, (balconyLength + toiletLength) / Length, (Width / 2 - doorWidth / 2) / Width),
                Offset(point, balconyLength / Length, (Width / 2 - doorWidth / 2) / Width),
            }, wallThickness, new ScriptReference[0]).Single();

            //Right of the corridor
            var _toiletRight = plan.AddRoom(new Vector2[]
            {
                Offset(point, balconyLength / Length, (Width / 2 + doorWidth / 2) / Width),
                Offset(point, (balconyLength + toiletLength) / Length, (Width / 2 + doorWidth / 2) / Width),
                Offset(point, (balconyLength + toiletLength) / Length, 1),
                Offset(point, balconyLength / Length, 1),
            }, wallThickness, new ScriptReference[0]).Single();

            //Corridor
            var corridorL = (Width / 2 - doorWidth / 2 + 0.01f) / Width;
            var corridorR = (Width / 2 + doorWidth / 2 - 0.01f) / Width;
            var _corridor = plan.AddRoom(new Vector2[]
            {
                Offset(point, balconyLength / Length, corridorL),
                Offset(point, (balconyLength + toiletLength) / Length, corridorL),
                Offset(point, (balconyLength + toiletLength) / Length, corridorR),
                Offset(point, balconyLength / Length, corridorR),
            }, wallThickness, new ScriptReference[0]).Single();

            //Add dining room
            var _diningRoom = plan.AddRoom(new Vector2[]
            {
                Offset(point, (balconyLength + toiletLength + 0.05f) / Length, 0),
                Offset(point, (Length - balconyLength - 0.05f) / Length, 0),
                Offset(point, (Length - balconyLength - 0.05f) / Length, 1),
                Offset(point, (balconyLength + toiletLength + 0.05f) / Length, 1),
            }, wallThickness, new ScriptReference[0]).Single();

            Assert.IsFalse(plan.GetNeighbours(_balcony2).Any(a => a.Other(_balcony2) != _diningRoom));

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(plan).ToString());
        }

        [TestMethod]
        public void FuzzTest()
        {
            Action<int, bool> iterate = (seed, catchit) =>
            {
                Random r = new Random(seed);

                try
                {
                    FloorPlan plan = new FloorPlan(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) });

                    for (int j = 0; j < 3; j++)
                    {
                        var minX = r.Next(-25, 20);
                        var minY = r.Next(-25, 20);
                        var width = r.Next(10, 20);
                        var height = r.Next(10, 20);
                        plan.AddRoom(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) },
                            1,
                            new ScriptReference[0]
                            );
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

            Random r = new Random(738);

            FloorPlan plan = new FloorPlan(new[] {new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25)});

            for (int j = 0; j < 3; j++)
            {
                var minX = r.Next(-25, 20);
                var minY = r.Next(-25, 20);
                var width = r.Next(10, 20);
                var height = r.Next(10, 20);
                plan.AddRoom(new[] {new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY)},
                    1,
                    new ScriptReference[0]
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

            var a = _plan.AddRoom(new[]
            {
                new Vector2(-100, -50),
                new Vector2(-100, 50),
                new Vector2(-70, 50),
                new Vector2(-70, -50),
            }, 5, new ScriptReference[0], false).Single();

            var b = _plan.AddRoom(new[]
            {
                new Vector2(-70, -50),
                new Vector2(-70, -10),
                new Vector2(-30, -10),
                new Vector2(-30, -50),
            }, 5, new ScriptReference[0], false).Single();

            var b2 = _plan.AddRoom(new[]
            {
                new Vector2(-30, 50),
                new Vector2(-30, 10),
                new Vector2(-70, 10),
                new Vector2(-70, 50),
            }, 5, new ScriptReference[0], false).Single();

            var c = _plan.AddRoom(new[]
            {
                new Vector2(-69.9f, -50),
                new Vector2(-69.9f, 50),
                new Vector2(60, 50),
                new Vector2(60, -50),
            }, 5, new ScriptReference[0], true).Single();

            var d = _plan.AddRoom(new[]
            {
                new Vector2(70, -50),
                new Vector2(70, 50),
                new Vector2(100, 50),
                new Vector2(100, -50),
            }, 5, new ScriptReference[0], false).Single();

            //A neighbours B, C, ~D
            var neighboursA = _plan.GetNeighbours(a);
            Assert.AreEqual(1, neighboursA.Count(x => x.Other(a) == b));
            Assert.AreEqual(1, neighboursA.Count(x => x.Other(a) == c));
            Assert.AreEqual(0, neighboursA.Count(x => x.Other(a) == d));

            //B neighbours A, C, ~D
            var neighboursB = _plan.GetNeighbours(b);
            Assert.AreEqual(1, neighboursB.Count(x => x.Other(b) == a));
            Assert.AreEqual(2, neighboursB.Count(x => x.Other(b) == c));
            Assert.AreEqual(0, neighboursB.Count(x => x.Other(b) == d));

            //C neighbours A, B, D
            var neighboursC = _plan.GetNeighbours(c);
            Assert.AreEqual(1, neighboursC.Count(x => x.Other(c) == a));
            Assert.AreEqual(2, neighboursC.Count(x => x.Other(c) == b));
            Assert.AreEqual(1, neighboursC.Count(x => x.Other(c) == d));

            //D neighbours ~A, ~B, C
            var neighboursD = _plan.GetNeighbours(d);
            Assert.AreEqual(0, neighboursD.Count(x => x.Other(d) == a));
            Assert.AreEqual(0, neighboursD.Count(x => x.Other(d) == b));
            Assert.AreEqual(1, neighboursD.Count(x => x.Other(d) == c));

            var duplicateCheck = c;
            var facades = duplicateCheck.GetFacades().ToArray();
            var duplicates = facades.Where(f => facades.Any(g => g != f && g.Section.Matches(f.Section))).ToArray();
            Assert.IsFalse(duplicates.Any());

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void RegressioTest_MissingFacadeStartSections()
        {
            // This is a test case found when designing trains
            // The start of a wall (from wall start -> start of first neighbour) didn't generate, now it does.
            // This test will fail if that happens again

            var b = _plan.AddRoom(new[]
            {
                new Vector2(-70, -50),
                new Vector2(-70, -10),
                new Vector2(-30, -10),
                new Vector2(-30, -50),
            }, 5, new ScriptReference[0], false).Single();

            var c = _plan.AddRoom(new[]
            {
                new Vector2(-69.9f, -50),
                new Vector2(-69.9f, 50),
                new Vector2(60, 50),
                new Vector2(60, -50),
            }, 5, new ScriptReference[0], true).Single();

            Assert.AreEqual(8, b.GetFacades().Count());
            Assert.AreEqual(14, c.GetFacades().Count());

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(_plan).ToString());
        }

        [TestMethod]
        public void RegressionTest_UnmatchedWallSections()
        {
            // This is a case generated from fuzz testing (i.e. generate random data, see what breaks).
            // Sometimes matching up inner and outer sections of wall data used to fail (fixed in EpimetheusPlugins).
            // This test will fail in that case

            Random r = new Random(189);

            FloorPlan plan = new FloorPlan(new[] { new Vector2(-25, -25), new Vector2(-25, 25), new Vector2(25, 25), new Vector2(25, -25) });

            for (int j = 0; j < 3; j++)
            {
                var minX = r.Next(-25, 20);
                var minY = r.Next(-25, 20);
                var width = r.Next(10, 20);
                var height = r.Next(10, 20);
                plan.AddRoom(new[] { new Vector2(minX, minY), new Vector2(minX, minY + height), new Vector2(minX + width, minY + height), new Vector2(minX + width, minY) },
                    1,
                    new ScriptReference[0]
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

            var v = new[] {new Vector2(15, 14), new Vector2(2, 14), new Vector2(2, 22), new Vector2(1, 22), new Vector2(1, 25), new Vector2(15, 25)};

            var r = _plan.AddRoom(v, 1, new ScriptReference[0], false);

            _plan.Freeze();

            var f = r.Single().GetFacades();

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
