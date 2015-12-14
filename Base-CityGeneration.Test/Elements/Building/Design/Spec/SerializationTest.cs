using System.Collections.Generic;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec
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
            var b = BuildingDesigner.Deserialize(new StringReader(@"
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
            var b = BuildingDesigner.Deserialize(new StringReader(@"
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
        1: { tag: a }
        2: { tag: b }
"));

            Assert.IsNotNull(b);
            Assert.AreEqual(1, b.FloorSelectors.Count());
            var spec = (FloorSpec)b.FloorSelectors.Single();

            var h = spec.Height;
            Assert.AreEqual(1, h.MinValue);
            Assert.AreEqual(5, h.MaxValue);

// ReSharper disable CompareOfFloatsByEqualityOperator
            Assert.AreEqual(new KeyValuePair<string, string>("tag", "a"), spec.Tags.Single(a => a.Key == 1).Value.Single());
            Assert.AreEqual(new KeyValuePair<string, string>("tag", "b"), spec.Tags.Single(a => a.Key == 2).Value.Single());
// ReSharper restore CompareOfFloatsByEqualityOperator
        }

        [TestMethod]
        public void DeserializeRange()
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
            Max: 1
          Vary: true
          Continuous: false
          Tags:
            1: { tag: a }
"));
        }

        [TestMethod]
        public void DeserializeMarker()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Ground []
"));

            Assert.IsInstanceOfType(b.FloorSelectors.Single(), typeof(GroundMarker));
        }
    }
}
