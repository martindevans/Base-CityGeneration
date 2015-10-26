using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
  - &FloorHeight !UniformValue
    Min: 2
    Max: 5
    Vary: true

Facades:
    # Penthouse facade
    - Tags: { 1: { tag: BlankFacade } }
      Bottom: !Id { Id: Penthouse }
      Top: !Id { Id: Penthouse, Inclusive: true }
    
    # Skylobby facades
    - Tags: { 1: { tag: BlankFacade } }
      Bottom: !Id { Id: Skylobby }
      Top: !Id { Id: Skylobby, Inclusive: true, Search: Up, Filter: First }
      
    # Residential facades
    - Tags: { 1: { tag: BlankFacade } }
      Bottom: !Id { Id: Residential }
      Top: !Id { Id: Residential, Inclusive: true, Search: Up, Filter: Longest, NonOverlapping: true }
      
    # Ground entrances
    - Tags: { 1: { tag: BlankFacade } }
      Bottom: !Num { N: 0 }
      Top: !Num { N: 0, Inclusive: true }
      #Constraints: [ !Access { Type: Road } ]
      
    - Tags: { 1: { tag: BlankFacade } }
      Bottom: !Num { N: 0 }
      Top: !Num { N: 0 }
      
Verticals:
  # First lift from ground->lowest skylobby
  - Tags: { 1: { tag: HollowVertical } }
    Bottom: !Num { N: 0 }
    Top: !Num { N: 5 }

Floors:
  - !Floor
    Id: Penthouse
    Tags: { 50: { tag: SolidFloor }, 50: null }

  - !Footprint
    - !Shrink { Distance: 5 }
    - !Twist { Angle: 15 }
    - !Clip {}

  - !Repeat
    Count: !NormalValue
      Min: 1
      Max: 5
    Items:

      - !Footprint
        - !Shrink { Distance: 5 }
        - !Twist { Angle: 15 }
        - !Clip {}

      - !Floor
        Id: Skylobby
        Tags: { 1: { tag: SolidFloor } }
      - !Repeat
        Count: *residential_floor_count
        Vary: true
        Items:
            - !Floor
              Tags: { 1: { tag: SolidFloor } }
              Id: Residential

  - !Footprint
    - !Shrink { Distance: 1 }
    - !Twist { Angle: 15 }
    - !Clip {}

  - !Floor
    Tags: { 1: { tag: SolidFloor } }
    Height: *FloorHeight

  - !Ground []

"));

            Assert.IsNotNull(b);

            Func<IEnumerable<KeyValuePair<string, string>>, ScriptReference> finder = tags => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tags));

            var lot = new Vector2[] {
                new Vector2(-30, -30),
                new Vector2(-30, 30f),
                new Vector2(30, 30f),
                new Vector2(30, -30f)
            };

            Random r = new Random(10);
            var selection = b.Internals(r.NextDouble, null, finder).Externals(r.NextDouble, null, finder, new BuildingSideInfo[] {
                new BuildingSideInfo(lot[0], lot[1], new BuildingSideInfo.NeighbourInfo[0]),
                new BuildingSideInfo(lot[1], lot[2], new BuildingSideInfo.NeighbourInfo[0]),
                new BuildingSideInfo(lot[2], lot[3], new BuildingSideInfo.NeighbourInfo[0]),
                new BuildingSideInfo(lot[3], lot[0], new BuildingSideInfo.NeighbourInfo[0]),
            });

            Assert.AreEqual(selection.Floors.Count(), selection.Floors.GroupBy(a => a.Index).Count());

            var v = selection.Verticals;
            Func<int, string> prefix = (floor) => new string(v.Select(a => a.Bottom <= floor && a.Top >= floor ? '|' : ' ').ToArray());

            foreach (var item in selection.Floors.OrderByDescending(a => a.Index))
            {
                var pre = prefix(item.Index);
                Console.WriteLine("{0} {1} {2:##.##}m", pre, item.Script.Name, item.Height);
            }
        }
    }
}
