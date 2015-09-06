using System.Linq;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class IdRefTest
    {
        readonly FloorSelection[] _floors = {
            new FloorSelection("top", null, null, null, 0, 12),
            new FloorSelection("a", null, null, null, 0, 11),
            new FloorSelection("a", null, null, null, 0, 10),
            new FloorSelection("a", null, null, null, 0, 9),
            new FloorSelection("b", null, null, null, 0, 8),
            new FloorSelection("c", null, null, null, 0, 7),
            new FloorSelection("d", null, null, null, 0, 6),
            new FloorSelection("e", null, null, null, 0, 5),
            new FloorSelection("f", null, null, null, 0, 4),
            new FloorSelection("g", null, null, null, 0, 3),
            new FloorSelection("g", null, null, null, 0, 2),
            new FloorSelection("g", null, null, null, 0, 1),
            new FloorSelection("ground", null, null, null, 0, 0),
            new FloorSelection("u1", null, null, null, 0, -1),
            new FloorSelection("u2", null, null, null, 0, -2),
        };

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingUpFromNull()
        {
            IdRef i = new IdRef("d", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingDownFromNull()
        {
            IdRef i = new IdRef("d", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingDownFromFloorAbove()
        {
            IdRef i = new IdRef("d", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, 12);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsNothing_SearchingDownFromFloorBelow()
        {
            IdRef i = new IdRef("d", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, 5);

            Assert.AreEqual(0, matches.Count());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsSingleMatch_SearchingDownFromFloorWithSameIdAsSearchId()
        {
            IdRef i = new IdRef("u1", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, -1);

            Assert.AreEqual(1, matches.Count());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsFloorWithGivenId_SearchingUpFromFloorBelow()
        {
            IdRef i = new IdRef("d", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, 5);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[6], matches.Single());
        }

        [TestMethod]
        public void AssertThat_IdRef_DoesNotFindFloorWithGivenId_SearchingUpFromFloorAbove()
        {
            IdRef i = new IdRef("d", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, 7);

            Assert.AreEqual(0, matches.Count());
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsLowestFloorFirst_SearchingUp()
        {
            IdRef i = new IdRef("a", SearchDirection.Up, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, 0).ToArray();

            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(9, matches[0].Index);
            Assert.AreEqual(10, matches[1].Index);
            Assert.AreEqual(11, matches[2].Index);
        }

        [TestMethod]
        public void AssertThat_IdRef_FindsHightestFloorFirst_SearchingDown()
        {
            IdRef i = new IdRef("g", SearchDirection.Down, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, 12).ToArray();

            Assert.AreEqual(3, matches.Count());
            Assert.AreEqual(3, matches[0].Index);
            Assert.AreEqual(2, matches[1].Index);
            Assert.AreEqual(1, matches[2].Index);
        }
    }
}
