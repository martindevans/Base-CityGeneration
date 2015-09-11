using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Aliases:
  - &residential_floor_count !NormalValue
    Min: 5
    Max: 10

Verticals:
    # First lift from ground->lowest skylobby
    - Tags: { 1: [lift] }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Skylobby, Search: Up, Filter: First }

    # Set of lifts from skylobby up to next skylobby
    - Tags: { 1: [lift] }
      Bottom: !Id { Id: Skylobby }
      Top: !Id { Id: Skylobby, Search: Up, Filter: First }

    # Express lift for penthouse
    - Tags: { 1: [lift] }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Penthouse }

Floors:
  - !Floor
    Tags: { 50: [roof, garden], 50: [roof, helipad] }
  - !Floor
    Id: Penthouse
    Tags: { 50: [penthouse], 50: null }
  - !Footprint
        - !Shrink { Distance: 1 }
  - !Repeat
    Count:
      !NormalValue
      Min: 1
      Max: 5
    Items:
      - !Floor
        Id: Skylobby
        Tags: { 1: [skylobby] }
      - !Range
        Includes:
          - Count: *residential_floor_count
            Tags: { 1: [apartment] }
  - !Floor
    Tags: { 1: [ground floor, lobby, shops] }
  - !Ground []
  - !Floor
    Tags: { 1: [parking] }
"));

            Assert.IsNotNull(b);

            Func<string[], ScriptReference> finder = tags => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tags));

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, finder);

            Assert.AreEqual(selection.Floors.Count(), selection.Floors.GroupBy(a => a.Index).Count());

            var v = selection.Verticals;
            Func<int, string> prefix = (floor) => new string(v.Select(a => a.Bottom <= floor && a.Top >= floor ? '|' : ' ').ToArray());

            foreach (var item in selection.AboveGroundFloors)
            {
                var pre = prefix(item.Index);
                Console.WriteLine("{0} {1} {2:##.##}m", pre, item.Script.Name, item.Height);
            }

            foreach (var item in selection.BelowGroundFloors)
            {
                var pre = prefix(item.Index);
                Console.WriteLine("{0} {1} {2:##.##}m", pre, item.Script.Name, item.Height);
            }
        }
    }
}
