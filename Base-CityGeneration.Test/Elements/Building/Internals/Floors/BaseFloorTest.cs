using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.TestHelpers.Scripts;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.Extensions;
using EpimetheusPlugins.Testing.MockProcedural;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TopologicalSorting;

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
                typeof(TestBuildingWithSingleFloor),
                typeof(TestBuildingWithSingleFloorWithInternalRoom),
                typeof(TestBuildingWithSingleFloorWithExternalRoom),

                typeof(BlankTestFloor),
                typeof(TestFloorWithSingleInternalRoom),
                typeof(TestFloorWithSingleExternalRoom),


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

            var building = result.Root.Children.OfType<BaseTestBuilding>().Single();
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

        [TestMethod]
        public void AssertThat_BaseFloor_CreatesInternalFacades()
        {
            var harness = new ProceduralTestHarness();

            var result = harness.Subdivide(new TestRoot(
                ScriptReference.Find<TestBuildingWithSingleFloorWithInternalRoom>().Single(),
                new Prism(100, new Vector2(0, 0), new Vector2(0, 100), new Vector2(100, 100), new Vector2(100, 0))
            ));

            Assert.IsNotNull(result);
            int index = 1;
            foreach (var dependency in (IEnumerable<ISet<OrderedProcess>>)result.Dependencies())
            {
                Console.WriteLine(index + ".");
                foreach (var orderedProcess in dependency)
                {
                    var n = ((OrderedProceduralNode)orderedProcess).Node;
                    var p = n.Parent;
                    Console.WriteLine(" - {0} ({1})", n.GetType().Name, p == null ? "null" : p.GetType().Name);
                }
                index++;
            }

            var building = result.Root.Children.OfType<BaseTestBuilding>().Single();
            var floor = building.Children.OfType<BaseFloor>().Single();
            var room = floor.Children.OfType<IPlannedRoom>().Single();

            //Check that 4 internal facades were created
            Assert.AreEqual(4, room.Facades.Count);
            foreach (var keyValuePair in room.Facades)
            {
                //Check the facades are in the right place
                Assert.IsTrue(keyValuePair.Value.Section.Matches(keyValuePair.Key.Section));

                //Check that the facades subdivide after rooms
                Assert.IsTrue(((ProceduralScript)keyValuePair.Value.GetDependencyContext()).Prerequisites().Contains((ProceduralScript)room));
            }
        }

        [TestMethod]
        public void AssertThat_BaseFloor_CreatesExternalFacades()
        {
            var harness = new ProceduralTestHarness();

            var result = harness.Subdivide(new TestRoot(
                ScriptReference.Find<TestBuildingWithSingleFloorWithExternalRoom>().Single(),
                new Prism(100, new Vector2(0, 0), new Vector2(0, 100), new Vector2(100, 100), new Vector2(100, 0))
            ));

            Assert.IsNotNull(result);

            var building = result.Root.Children.OfType<BaseTestBuilding>().Single();
            var floor = building.Children.OfType<BaseFloor>().Single();
            var room = floor.Children.OfType<IPlannedRoom>().Single();

            //Check that 4 room facades were created
            Assert.AreEqual(4, room.Facades.Count);

            //Check that facades are all in the correct locations
            foreach (var keyValuePair in room.Facades)
            {
                //Check the facades are in the right place
                Assert.IsTrue(keyValuePair.Value.Section.Matches(keyValuePair.Key.Section));

                //Check that the facades subdivide after rooms
                Assert.IsTrue(((ProceduralScript)keyValuePair.Value.GetDependencyContext()).Prerequisites().Contains((ProceduralScript)room));
            }

            //Check that 2 external facades were created
            Assert.AreEqual(2, room.Facades.Count(f => f.Key.IsExternal));
        }
    }

    [Script("2109B59A-077D-4877-885E-2C60FD948CB4", "Test Building: Single Floor")]
    public class TestBuildingWithSingleFloor
        : BaseTestBuilding
    {
        private static readonly FloorSelection[] _floors = {
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<BlankTestFloor>().First(), 2, 0),
        };

        private static readonly VerticalSelection[] _verticals = {
        };

        public TestBuildingWithSingleFloor()
            : base(_floors, _verticals)
        {
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

    [Script("A70AC267-08D7-42D1-BC19-9646C45998E5", "Test Building: Single Floor With Internal Room")]
    public class TestBuildingWithSingleFloorWithInternalRoom
        : BaseTestBuilding
    {
        private static readonly FloorSelection[] _floors = {
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<TestFloorWithSingleInternalRoom>().First(), 2, 0),
        };

        private static readonly VerticalSelection[] _verticals = { };

        public TestBuildingWithSingleFloorWithInternalRoom()
            : base(_floors, _verticals)
        {
        }
    }

    [Script("EF405947-604F-4C79-8BF2-4BC66C246DD4" ,"Test Floor: Single Internal Room")]
    public class TestFloorWithSingleInternalRoom
        : BaseTestFloor
    {
        private static readonly Vector2[] _room = {
            new Vector2(20, 20),
            new Vector2(30, 20),
            new Vector2(30, 10),
            new Vector2(20, 10)
        };

        public TestFloorWithSingleInternalRoom()
            : base(_room)
        {
        }
    }

    [Script("E9256AF6-155A-4604-9E3B-59A55BE5A92D", "Test Building: Single Floor With External Room")]
    public class TestBuildingWithSingleFloorWithExternalRoom
        : BaseTestBuilding
    {
        private static readonly FloorSelection[] _floors = {
            new FloorSelection("g", Array.Empty<KeyValuePair<string, string>>(), ScriptReference.Find<TestFloorWithSingleExternalRoom>().First(), 2, 0),
        };

        private static readonly VerticalSelection[] _verticals = { };

        public TestBuildingWithSingleFloorWithExternalRoom()
            : base(_floors, _verticals)
        {
        }
    }

    [Script("DB6B8938-8582-4296-AD83-F1C3AD114D1E", "Test Floor: Single External Room")]
    public class TestFloorWithSingleExternalRoom
        : BaseTestFloor
    {
        private static readonly Vector2[] _room = {
            new Vector2(0, 10),
            new Vector2(10, 10),
            new Vector2(10, 0),
            new Vector2(0, 0)
        };

        public TestFloorWithSingleExternalRoom()
            : base(_room)
        {
        }
    }
}
