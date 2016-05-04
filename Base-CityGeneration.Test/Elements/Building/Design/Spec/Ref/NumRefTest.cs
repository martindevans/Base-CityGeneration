using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.Ref;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Ref
{
    [TestClass]
    public class NumRefTest
    {
        private static FloorSelection CreateFloor(string name, float height, int number)
        {
            return new FloorSelection(
                name,
                new KeyValuePair<string, string>[0],
                new ScriptReference(typeof(TestScript)),
                height,
                number
            );
        }

        readonly FloorSelection[] _floors = {
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
            CreateFloor("u", 0, -1),
            CreateFloor("u", 0, -2),
        };

        [TestMethod]
        public void AssertThat_NumRef_FindsSpecifiedNumber()
        {
            NumRef i = new NumRef(10, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[2], matches.Single());  //NumRef 10 == Floor 2 because we're counting *up* from the ground floor
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsBasement()
        {
            NumRef i = new NumRef(-2, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(1, matches.Count());
            Assert.AreEqual(_floors[14], matches.Single());
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsNothing_WhenNumberIsTooLow()
        {
            NumRef i = new NumRef(-3, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(0, matches.Count());
        }

        [TestMethod]
        public void AssertThat_NumRef_FindsNothing_WhenNumberIsTooHigh()
        {
            NumRef i = new NumRef(13, RefFilter.All, false, false);

            var matches = i.Match(_floors, null);

            Assert.AreEqual(0, matches.Count());
        }
    }
}
