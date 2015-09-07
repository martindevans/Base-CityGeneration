using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class NumRefTest
    {
        readonly FloorSelection[] _floors = {
            new FloorSelection("top", null, null, null, 0, 12),
            new FloorSelection("a", null, null, null, 0, 11),
            new FloorSelection("a", null, null, null, 0, 10),
            new FloorSelection("a", null, null, null, 0, 9),
            new FloorSelection("b", null, null, null, 0, 8),
            new FloorSelection("c", null, null, null, 0, 7),
            new FloorSelection("d", null, null, null,  0, 6),
            new FloorSelection("e", null, null, null, 0, 5),
            new FloorSelection("f", null, null, null, 0, 4),
            new FloorSelection("g", null, null, null, 0, 3),
            new FloorSelection("g", null, null, null, 0, 2),
            new FloorSelection("g", null, null, null, 0, 1),
            new FloorSelection("ground", null, null, null, 0, 0),
            new FloorSelection("u", null, null, null, 0, -1),
            new FloorSelection("u", null, null, null, 0, -2),
        };

        [TestMethod]
        public void AssertThat_NumRef_FindsSpecifiedNumber()
        {
            NumRef i = new NumRef(10, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[2], matches.Single());  //NumRef 10 == Floor 2 because we're counting *up* from the ground floor
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsBasement()
        {
            NumRef i = new NumRef(-2, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[14], matches.Single());
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsNothing_WhenNumberIsTooLow()
        {
            NumRef i = new NumRef(-3, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(0, matches.Count());
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsNothing_WhenNumberIsTooHigh()
        {
            NumRef i = new NumRef(13, RefFilter.All, false, false);

            var matches = i.Match(2, _floors, null);

            Assert.AreEqual(0, matches.Count());
        }
    }
}
