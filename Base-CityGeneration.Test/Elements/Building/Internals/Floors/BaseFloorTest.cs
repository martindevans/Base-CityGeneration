using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.TestHelpers.Scripts;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.Extensions;
using EpimetheusPlugins.Testing.MockProcedural;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors
{
    [TestClass]
    public class BaseFloorTest
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ScriptReferenceExtensions.TestInitialize(
                typeof(TestBuildingWithVerticals),
                typeof(BlankTestFloor),
                typeof(DefaultTestFacade),
                typeof(BlankTestVertical)
            );
        }

        [TestMethod]
        public void AssertThat_BaseFloor_CreatesVerticals()
        {
            var harness = new ProceduralTestHarness();

            var result = harness.Subdivide(new TestRoot(
                ScriptReference.Find<TestBuildingWithVerticals>().Single(),
                new Prism(100, new Vector2(0, 0), new Vector2(0, 100), new Vector2(100, 100), new Vector2(100, 0))
            ));

            Assert.IsNotNull(result);

            var building = result.Children.OfType<BaseTestBuilding>().Single();
            var vertical = building.Children.SelectMany(c => c.Children).OfType<IVerticalFeature>().Single();

            //Vertical from 1 -> 3
            Assert.AreEqual(1, vertical.BottomFloorIndex);
            Assert.AreEqual(3, vertical.TopFloorIndex);

            //Did all the floors in the range get informed of this element?
            Assert.AreEqual(0, ((BlankTestFloor)building.Floor(0)).OverlappingVerticals.Count());
            Assert.AreEqual(0, ((BlankTestFloor)building.Floor(1)).OverlappingVerticals.Count());   //Floor 1 placed the element, so it is *not* informed of it this way!
            Assert.AreEqual(1, ((BlankTestFloor)building.Floor(2)).OverlappingVerticals.Count());
            Assert.AreEqual(1, ((BlankTestFloor)building.Floor(3)).OverlappingVerticals.Count());
            Assert.AreEqual(0, ((BlankTestFloor)building.Floor(4)).OverlappingVerticals.Count());

            //Does vertical container contain vertical for all these floors
            var vContainer = (IVerticalFeatureContainer)building;
            Assert.AreEqual(0, vContainer.Overlapping(0).Count());
            Assert.AreEqual(1, vContainer.Overlapping(1).Count());
            Assert.AreEqual(1, vContainer.Overlapping(2).Count());
            Assert.AreEqual(1, vContainer.Overlapping(3).Count());
            Assert.AreEqual(0, vContainer.Overlapping(4).Count());
        }
    }

    [Script("6DC1553B-5925-4E16-ABA8-1518A5AE5F66", "Test Building: Vertical Elements")]
    public class TestBuildingWithVerticals
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
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 4),
        };

        private static readonly VerticalSelection[] _verticals = {
            new VerticalSelection(ScriptReference.Find<BlankTestVertical>().Single(), 1, 3)
        };

        public TestBuildingWithVerticals()
            : base(_floors, _verticals)
        {
        }
    }
}
