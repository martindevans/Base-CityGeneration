using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec.Markers;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection.Spec
{
    /// <summary>
    /// Summary description for SpecTest
    /// </summary>
    [TestClass]
    public class SerializationTest
    {
        [TestMethod]
        public void DeserializeBuilding()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors: []
"));

            Assert.IsNotNull(b);
            Assert.AreEqual(0, b.FloorSelectors.Count());
        }

        [TestMethod]
        public void DeserializeFloor()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor
      Height:
        !NormalValue
        Min: 1
        Mean: 3
        Max: 5
        Deviation: 2

      Tags:
        1: [a]
        2: [b]
"));

            Assert.IsNotNull(b);
            Assert.AreEqual(1, b.FloorSelectors.Count());
            var spec = (FloorSpec)b.FloorSelectors.Single();

            var h = (NormallyDistributedValue) spec.Height;
            Assert.AreEqual(1, h.Min);
            Assert.AreEqual(5, h.Max);
            Assert.AreEqual(3, h.Mean);
            Assert.AreEqual(2, h.Deviation);

// ReSharper disable CompareOfFloatsByEqualityOperator
            Assert.AreEqual("a", spec.Tags.Single(a => a.Key == 1).Value.Single());
            Assert.AreEqual("b", spec.Tags.Single(a => a.Key == 2).Value.Single());
// ReSharper restore CompareOfFloatsByEqualityOperator
        }

        [TestMethod]
        public void DeserializeRange()
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
            Max: 1
          Vary: true
          Continuous: false
          Tags:
            1: [a]
"));
        }

        [TestMethod]
        public void DeserializeMarker()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Ground {}
"));

            Assert.IsInstanceOfType(b.FloorSelectors.Single(), typeof(GroundMarker));
        }
    }
}
