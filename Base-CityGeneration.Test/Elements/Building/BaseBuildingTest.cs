using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.TestHelpers.Scripts;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.Extensions;
using EpimetheusPlugins.Testing.MockProcedural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building
{
    [TestClass]
    public class BaseBuildingTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ScriptReferenceExtensions.TestInitialize(
                typeof(TestBuildingWithTwoFloors),
                typeof(TestBuildingWithManyFloors),
                typeof(BlankTestFloor),
                typeof(DefaultTestFacade)
            );
        }

        [TestMethod]
        public void AssertThat_BaseBuilding_CreatesSelectedFloors_WithAboveGroundFloors()
        {
            var harness = new ProceduralTestHarness();

            var result = harness.Subdivide(new TestRoot(
                ScriptReference.Find<TestBuildingWithTwoFloors>().Single(),
                new Prism(100, new Vector2(0, 0), new Vector2(0, 100), new Vector2(100, 100), new Vector2(100, 0))
            ));

            Assert.IsNotNull(result);

            var building = result.Children.OfType<BaseTestBuilding>().Single();
            Assert.IsNotNull(building.Floor(0));
            Assert.IsNotNull(building.Floor(-1));
            Assert.AreEqual(1, building.AboveGroundFloors);
            Assert.AreEqual(1, building.BelowGroundFloors);
            Assert.AreEqual(2, building.TotalFloors);
        }

        [TestMethod]
        public void AssertThat_BaseBuilding_CreatesPrerequisiteRelationships()
        {
            var harness = new ProceduralTestHarness();

            var result = harness.Subdivide(new TestRoot(
                ScriptReference.Find<TestBuildingWithManyFloors>().Single(),
                new Prism(100, new Vector2(0, 0), new Vector2(0, 100), new Vector2(100, 100), new Vector2(100, 0))
            ));

            Assert.IsNotNull(result);

            var building = result.Children.OfType<BaseTestBuilding>().Single();
            Assert.IsNotNull(building.Floor(0));
            Assert.IsNotNull(building.Floor(-1));
            Assert.AreEqual(4, building.AboveGroundFloors);
            Assert.AreEqual(4, building.BelowGroundFloors);
            Assert.AreEqual(8, building.TotalFloors);

            //Check that all above ground floors depend on the floor below
            var above = building.Children
                .OfType<BlankTestFloor>()
                .Where(a => a.FloorIndex >= 0)
                .OrderBy(a => a.FloorIndex)
                .ToArray();
            for (var i = 1; i < above.Length; i++)
            {
                var prereq = above[i].Prerequisites().ToArray();
                Assert.IsTrue(prereq.Contains(above[i - 1]));
            }

            //Check that all below ground floors depend on the floor above
            var below = building.Children
                .OfType<BlankTestFloor>()
                .Where(a => a.FloorIndex <= 0)
                .OrderBy(a => -a.FloorIndex)
                .ToArray();
            for (var i = 1; i < below.Length; i++)
            {
                var prereq = below[i].Prerequisites().ToArray();
                Assert.IsTrue(prereq.Contains(below[i - 1]));
            }
        }
    }

    [Script("5E8D2C89-FD4F-43B2-B571-DE71A5A713DF", "Test Building: Two Floors")]
    public class TestBuildingWithTwoFloors
        : BaseTestBuilding
    {
        private static readonly FloorSelection[] _floors = {
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, -1),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 0)
        };

        public TestBuildingWithTwoFloors()
            : base(_floors, Array.Empty<VerticalSelection>())
        {
        }
    }

    [Script("6DC1553B-5925-4E16-ABA8-1518A5AE5F66", "Test Building: Many Floors")]
    public class TestBuildingWithManyFloors
        : BaseTestBuilding
    {
        private static readonly FloorSelection[] _floors = {
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, -4),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, -3),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, -2),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, -1),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 0),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 1),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 2),
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 3),
        };

        public TestBuildingWithManyFloors()
            : base(_floors, Array.Empty<VerticalSelection>())
        {
        }
    }
}
