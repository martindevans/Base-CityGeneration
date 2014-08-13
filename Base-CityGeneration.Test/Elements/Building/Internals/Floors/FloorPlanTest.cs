using System;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors
{
    [TestClass]
    public class FloorPlanTest
    {
        private FloorPlan _plan;

        [TestInitialize]
        public void Initialize()
        {
            _plan = new FloorPlan(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(100, 100), new Vector2(100, -100) }, 0);
        }

        [TestMethod]
        public void RoomInternalBordersAreSmaller()
        {
            var r = _plan.AddRoom(new Vector2[]
            {
                new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10)
            }, 0.1f, new ScriptReference[0], false).Single();

            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(-9.9f, 9.9f)));
            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(-9.9f, -9.9f)));
            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(9.9f, 9.9f)));
            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(9.9f, -9.9f)));
        }

        [TestMethod]
        public void RoomInternalBordersAreSmallerWhenNotAtOrigin()
        {
            var r = _plan.AddRoom(new Vector2[]
            {
                new Vector2(10, 10), new Vector2(10, 30), new Vector2(30, 30), new Vector2(30, 10)
            }, 0.1f, new ScriptReference[0], false).Single();

            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(10.1f, 29.9f)));
            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(10.1f, 10.1f)));
            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(10.1f, 10.1f)));
            Assert.IsTrue(r.InnerFootprint.Contains(new Vector2(29.9f, 10.1f)));
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
            FloorPlan.RoomInfo wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //Low room
            FloorPlan.RoomInfo low = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide);
            Assert.AreEqual(1, wideNeighbours.Count());

            //Check that points C and D lies on the edge of low room
            var n = wideNeighbours.Single(a => a.RoomCD == low);
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.C, new Line2D(new Vector2(-10, -90.1f), new Vector2(1, 0))) < 0.01f);
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.D, new Line2D(new Vector2(-10, -90.1f), new Vector2(1, 0))) < 0.01f);

            //Check that points A and B lie on the edge of wide room
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.A, new Line2D(new Vector2(-10, -9.9f), new Vector2(1, 0))) < 0.01f);
            Assert.IsTrue(Geometry2D.DistanceFromPointToLine(n.B, new Line2D(new Vector2(-10, -9.9f), new Vector2(1, 0))) < 0.01f);

            //Check that neighbour data is the same going the other direction
            var lowNeighbours = _plan.GetNeighbours(low);
            Assert.AreEqual(1, lowNeighbours.Count());
            Assert.IsTrue(lowNeighbours.Any(a => a.RoomCD == wide));

            //Check that point is close to the edge it is supposed to lie on
            var segment = new LineSegment2D(wide.InnerFootprint[n.EdgeIndexRoomAB], wide.InnerFootprint[(n.EdgeIndexRoomAB + 1) % wide.InnerFootprint.Length]);
            var line = segment.Line();
            var dist = Geometry2D.DistanceFromPointToLine(n.A, line);
            Assert.IsTrue(dist < 0.01f);

            //Check that point is close to the edge it is supposed to lie on
            var segment2 = new LineSegment2D(low.InnerFootprint[n.EdgeIndexRoomCD], low.InnerFootprint[(n.EdgeIndexRoomCD + 1) % low.InnerFootprint.Length]);
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
        public void RoomNeighbourInfoIsCorrectlyWound()
        {
            //A really wide room
            FloorPlan.RoomInfo wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            FloorPlan.RoomInfo high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            _plan.Freeze();

            var wideNeighbours = _plan.GetNeighbours(wide).Single();

            Assert.IsTrue(new[] { wideNeighbours.A, wideNeighbours.B, wideNeighbours.C, wideNeighbours.D }.Area() > 0);
        }

        [TestMethod]
        [Timeout(1000)]
        public void GetRoomNeighboursFindsBasicPairReversed()
        {
            //A really wide room
            FloorPlan.RoomInfo wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            FloorPlan.RoomInfo high = _plan.AddRoom(
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


            FloorPlan.RoomInfo a = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            FloorPlan.RoomInfo b = _plan.AddRoom(
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

            Assert.IsTrue(a.InnerFootprint.Where(p => Vector2.Distance(p, n1.A) < 0.1f).Any());
            Assert.IsTrue(b.InnerFootprint.Where(p => Vector2.Distance(p, n1.C) < 0.1f).Any());
        }

        [TestMethod]
        public void DisjointRoomsAreHandled()
        {
            //A really wide room
            FloorPlan.RoomInfo a = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -10), new Vector2(-10, 10), new Vector2(10, 10), new Vector2(10, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            FloorPlan.RoomInfo b = _plan.AddRoom(
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
            //A really wide room
            FloorPlan.RoomInfo wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //Low room
            FloorPlan.RoomInfo low = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            FloorPlan.RoomInfo high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

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
            FloorPlan.RoomInfo wide = _plan.AddRoom(
                new Vector2[] { new Vector2(-100, -10), new Vector2(-100, 10), new Vector2(100, 10), new Vector2(100, -10) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //Low room
            FloorPlan.RoomInfo low = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, -100), new Vector2(-10, -90), new Vector2(10, -90), new Vector2(10, -100) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High room
            FloorPlan.RoomInfo high = _plan.AddRoom(
                new Vector2[] { new Vector2(-10, 100), new Vector2(10, 100), new Vector2(10, 90), new Vector2(-10, 90) },
                0.1f,
                new ScriptReference[0]
            ).Single();

            //High left
            FloorPlan.RoomInfo highLeft = _plan.AddRoom(
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
            var room = _plan.AddRoom(_plan.Footprint, 0.25f, new ScriptReference[0]).Single();

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
            var roomA = _plan.AddRoom(new Vector2[] { new Vector2(-100, -100), new Vector2(-100, 100), new Vector2(0, 100), new Vector2(0, -100) }, 0.25f, new ScriptReference[0]).Single();
            var roomB = _plan.AddRoom(new Vector2[] { new Vector2(10, -10), new Vector2(10, 10), new Vector2(20, 10), new Vector2(20, -10) }, 0.25f, new ScriptReference[0]).Single();

            _plan.Freeze();

            var facades = roomA.GetFacades().ToArray();

            var internalFacades = facades.Where(f => !f.IsExternal).ToArray();

            Assert.AreEqual(1, internalFacades.Where(f => f.NeighbouringRoom == roomB).Count());
            Assert.AreEqual(3, internalFacades.Count());

            //Check that the neighbour section has points lying on both rooms
            var n = internalFacades.Where(f => f.NeighbouringRoom == roomB).Single();
            //Assert.IsTrue(roomB.InnerFootprint.Where(p => Vector2.Distance(p, n.Section.C) < 0.1f).Any());
            //Assert.IsTrue(roomA.InnerFootprint.Where(p => Vector2.Distance(p, n.Section.A) < 0.1f).Any());

            //Check that section has points adjacent to right edge of room A
            var segment = new LineSegment2D(roomA.InnerFootprint[3], roomA.InnerFootprint[0]);
            var line = segment.Line();
            var dist = Geometry2D.DistanceFromPointToLine(n.Section.A, line);
            Assert.IsTrue(dist < 0.01f);

            //Check that section has points adjacent to left edge of room B
            var segment2 = new LineSegment2D(roomB.InnerFootprint[1], roomB.InnerFootprint[2]);
            var line2 = segment2.Line();
            var dist2 = Geometry2D.DistanceFromPointToLine(n.Section.C, line2);
            Assert.IsTrue(dist2 < 0.01f);
        }
    }
}
