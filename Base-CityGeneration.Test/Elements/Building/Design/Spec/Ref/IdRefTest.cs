using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class IdRefTest
    {
        private static FloorSelection CreateFloor(string name, float height, int number)
        {
            return new FloorSelection(name, new KeyValuePair<string, string>[0], new FootprintMarker(new BaseFootprintAlgorithm[0]), new ScriptReference(typeof(TestScript)), height, number);
        }

        private readonly FloorSelection[] _floors = {
            CreateFloor("top", 0, 12),
            CreateFloor("a", 0, 11),
            CreateFloor("a", 0, 10),
            CreateFloor("a", 0, 9),
            CreateFloor("b", 0, 8),
            CreateFloor("c", 0, 7),
            CreateFloor("d", 0, 6),
            CreateFloor("e", 0, 5),
            CreateFloor("f", 0, 4),
            CreateFloor("g", 0, 3),
            CreateFloor("g", 0, 2),
            CreateFloor("g", 0, 1),
            CreateFloor("ground", 0, 0),
            CreateFloor("u1", 0, -1),
            CreateFloor("u2", 0, -2),
        };

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingUpFromNull()
        {
            IdRef i = new IdRef("d", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingDownFromNull()
        {
            IdRef i = new IdRef("d", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingDownFromFloorAbove()
        {
            IdRef i = new IdRef("d", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(_floors, 12);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsNothing_SearchingDownFromFloorBelow()
        {
            IdRef i = new IdRef("d", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(_floors, 5);

            Assert.AreEqual(0, matches.Count());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsSingleMatch_SearchingDownFromFloorWithSameIdAsSearchId()
        {
            IdRef i = new IdRef("u1", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(_floors, -1);

            Assert.AreEqual(1, matches.Count());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingUpFromFloorBelow()
        {
            IdRef i = new IdRef("d", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(_floors, 5);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_DoesNotFindFloorWithGivenId_SearchingUpFromFloorAbove()
        {
            IdRef i = new IdRef("d", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(_floors, 7);

            Assert.AreEqual(0, matches.Count());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsLowestFloorFirst_SearchingUp()
        {
            IdRef i = new IdRef("a", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(_floors, 0).ToArray();

            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(9, matches[0].Index);
            Assert.AreEqual(10, matches[1].Index);
            Assert.AreEqual(11, matches[2].Index);
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsHightestFloorFirst_SearchingDown()
        {
            IdRef i = new IdRef("g", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(_floors, 12).ToArray();

            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(3, matches[0].Index);
            Assert.AreEqual(2, matches[1].Index);
            Assert.AreEqual(1, matches[2].Index);
        }
    }
}
