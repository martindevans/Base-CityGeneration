using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design
{
    [TestClass]
    public class BuildingDesignerTest
    {
        private static ScriptReference Finder(string[] tags)
        {
            Assert.IsNotNull(tags);
            return new ScriptReference(typeof(TestScript));
        }

        [TestMethod]
        public void AssertThat_SingleFloorBuilding_OutputsSingleFloor()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor
      Height:
        !UniformValue
        Min: 1
        Max: 2
      Tags:
        1: [a]
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Select(r.NextDouble, null, Finder, new ReadOnlyCollection<float>(new float[] { }));

            Assert.AreEqual(1, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());

            var h = selection.AboveGroundFloors.Single().Height;
            Assert.IsTrue(h >= 1 && h <= 2);
        }

        [TestMethod]
        public void AssertThat_TwoFloors_WithHeightGroup_InheritsHeightFromRootGroup()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Aliases:
    - &groupname !NormalValue
      Vary: false
      Min: 5
      Max: 10
Verticals: []
Floors:
    - !Floor
      Height: *groupname
      Tags:
        1: [a]
    - !Floor
      Height: *groupname
      Tags:
        1: [a]
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Select(r.NextDouble, null, Finder, new ReadOnlyCollection<float>(new float[] { }));

            Assert.AreEqual(2, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());

            var h = selection.AboveGroundFloors.First().Height;
            Assert.IsTrue(h >= 5f && h <= 10f);

            var h2 = selection.AboveGroundFloors.Skip(1).First().Height;
            Assert.AreEqual(h, h2);
        }

        [TestMethod]
        public void AssertThat_SingleFloorBuilding_WithNullFloor_OutputsNoFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor
      Tags:
        1: null
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Select(r.NextDouble, null, Finder, new ReadOnlyCollection<float>(new float[] { }));

            Assert.AreEqual(0, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void AssertThat_MultiFloorSelector_OutputsMultipleFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Range
      Includes:
        - Count:
            !UniformValue
            Min: 1
            Max: 5
          Vary: true
          Tags:
            1: [a]
            1: [b]
            0: null
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Select(r.NextDouble, null, Finder, new ReadOnlyCollection<float>(new float[] { }));

            Assert.IsTrue(selection.AboveGroundFloors.Count >= 1 && selection.AboveGroundFloors.Count <= 5);
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacade()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: [test] }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Top, Search: Up, Filter: Longest, NonOverlapping: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: F, Tags: { 1: [a] } }
    - !Floor { Id: Bot, Tags: { 1: [a] } }
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Select(r.NextDouble, null, Finder, new ReadOnlyCollection<float>(new float[] { 1 }));

            Assert.AreEqual(1, selection.Facades.Count);
            Assert.AreEqual(1, selection.Facades.Single().Count);
        }

        [TestMethod]
        public void Playground()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals:
    - Tags: { 1: [lift] }
      Bottom: !Id { Id: GroundFloor }
      Top: !Id { Id: TopFloor }
Floors:
    - !Floor { Id: TopFloor, Tags: { 1: [a] } }
    - !Floor { Tags: { 1: [a] } }
    - !Floor { Id: GroundFloor, Tags: { 1: [a] } }
"));

            Func<string[], ScriptReference> finder = tags => {
                Assert.IsNotNull(tags);
                return new ScriptReference(typeof(TestScript));
            };

            Random r = new Random();
            var selection = b.Select(r.NextDouble, null, finder, new ReadOnlyCollection<float>(new float[] { }));

            Assert.AreEqual(3, selection.AboveGroundFloors.Count());
            Assert.AreEqual(1, selection.Verticals.Count());
        }
    }

    [Script("93863CB4-2951-453E-95B8-955077322550", "Test")]
    internal class TestScript
    {
    }
}
