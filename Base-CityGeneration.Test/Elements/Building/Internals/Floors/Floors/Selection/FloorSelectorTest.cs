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
      Tags:
        1: [a]
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Select(r.NextDouble, (tags) => new ScriptReference(typeof(TestScript)));

            Assert.AreEqual(1, selection.Floors.Count());
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
            var selection = b.Select(r.NextDouble, (tags) => new ScriptReference(typeof(TestScript)));

            Assert.AreEqual(0, selection.Floors.Count());
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
        - AtLeast: 1
          AtMost: 5
          Vary: true
          Chance: 100
          Tags:
            1: [a]
            1: [b]
            0: null
"));

            Assert.IsNotNull(b);

            Func<string[], ScriptReference> finder = (tags) => {
                Assert.IsNotNull(tags);
                return new ScriptReference(typeof(TestScript));
            };

            Random r = new Random();
            var selection = b.Select(r.NextDouble, finder);

            Assert.IsTrue(selection.Floors.Length >= 1 && selection.Floors.Length <= 5);
        }
    }

    [Script("93863CB4-2951-453E-95B8-955077322550", "Test")]
    internal class TestScript
    {
    }
}
