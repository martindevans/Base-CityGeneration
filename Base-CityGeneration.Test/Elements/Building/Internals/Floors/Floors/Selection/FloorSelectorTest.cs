using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection
{
    [TestClass]
    public class FloorSelectorTest
    {
        [TestMethod]
        public void AssertThat_SingleFloorBuilding_OutputsSingleFloor()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
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
            var selection = b.Select(r.NextDouble, tags => new ScriptReference(typeof(TestScript)));

            Assert.AreEqual(1, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());

            var h = selection.AboveGroundFloors.Single().Height;
            Assert.IsTrue(h >= 1 && h <= 2);
        }

        [TestMethod]
        public void AssertThat_TwoFloors_WithHeightGroup_InheritsHeightFromRootGroup()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
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
            var selection = b.Select(r.NextDouble, tags => new ScriptReference(typeof(TestScript)));

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
            var b = FloorSelector.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor
      Tags:
        1: null
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Select(r.NextDouble, tags => new ScriptReference(typeof(TestScript)));

            Assert.AreEqual(0, selection.AboveGroundFloors.Count());
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void AssertThat_MultiFloorSelector_OutputsMultipleFloors()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
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

            Func<string[], ScriptReference> finder = tags => {
                Assert.IsNotNull(tags);
                return new ScriptReference(typeof(TestScript));
            };

            Random r = new Random();
            var selection = b.Select(r.NextDouble, finder);

            Assert.IsTrue(selection.AboveGroundFloors.Length >= 1 && selection.AboveGroundFloors.Length <= 5);
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void MethodName()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
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
            var selection = b.Select(r.NextDouble, finder);

            Assert.AreEqual(3, selection.AboveGroundFloors.Count());
            Assert.AreEqual(1, selection.Verticals.Count());
        }
    }

    [Script("93863CB4-2951-453E-95B8-955077322550", "Test")]
    internal class TestScript
    {
    }
}
