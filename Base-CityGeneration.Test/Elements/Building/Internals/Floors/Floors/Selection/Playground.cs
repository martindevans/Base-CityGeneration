using System;
using System.IO;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
!Building
Groups:
  residential_floor_count:
    Min: 5
    Max: 10
Verticals: []
Floors:
  - !Floor
    Tags: { 50: [roof, garden], 50: [roof, helipad] }
  - !Floor
    Tags: { 50: [penthouse], 50: null }
  - !Repeat
    Count:
      Min: 1
      Max: 5
    Items:
      - !Floor
        Tags: { 1: [skylobby] }
      - !Range
        Includes:
          - Count: { Group: residential_floor_count }
            Tags: { 1: [apartment] }
  - !Floor
    Tags: { 1: [ground floor, lobby, shops] }
  - !Ground {}
"));

            Assert.IsNotNull(b);

            Func<string[], ScriptReference> finder = tags => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tags));

            Random r = new Random();
            var selection = b.Select(r.NextDouble, finder);

            foreach (var item in selection.AboveGroundFloors)
                Console.WriteLine("{0} {1:##.##}m", item.Script.Name, item.Height);
            foreach (var item in selection.BelowGroundFloors)
                Console.WriteLine("{0} {1:##.##}m", item.Script.Name, item.Height);
        }
    }
}
