using System;
using System.IO;
using System.Linq;
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
  - !Ground {}
  - !Floor
    Tags: { 1: [parking] }
"));

            Assert.IsNotNull(b);

            Func<string[], ScriptReference> finder = tags => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tags));

            Random r = new Random();
            var selection = b.Select(r.NextDouble, finder);

            var v = selection.Verticals;
            Func<int, string> prefix = (floor) => new string(v.Select(a => a.Bottom <= floor && a.Top >= floor ? '|' : ' ').ToArray());

            for (int i = 0; i < selection.AboveGroundFloors.Length; i++)
            {
                var item = selection.AboveGroundFloors[i];
                var pre = prefix(selection.AboveGroundFloors.Length - i - 1);
                Console.WriteLine("{0} {1} {2:##.##}m", pre, item.Script.Name, item.Height);
            }

            for (int i = 0; i < selection.BelowGroundFloors.Length; i++)
            {
                var item = selection.BelowGroundFloors[i];
                var pre = prefix(-(i + 1));
                Console.WriteLine("{0} {1} {2:##.##}m", pre, item.Script.Name, item.Height);
            }
        }
    }
}
