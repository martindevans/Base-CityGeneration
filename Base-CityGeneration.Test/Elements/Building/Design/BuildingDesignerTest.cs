using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers;
using EpimetheusPlugins.Scripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Test.Elements.Building.Design
{
    [TestClass]
    public class BuildingDesignerTest
    {
        private static readonly BuildingSideInfo[] _noNeighbours = {
            new BuildingSideInfo(new Vector2(-10, -10), new Vector2(10, -10), new BuildingSideInfo.NeighbourInfo[0]),
            new BuildingSideInfo(new Vector2(10, -10), new Vector2(10, 10), new BuildingSideInfo.NeighbourInfo[0]),
            new BuildingSideInfo(new Vector2(10, 10), new Vector2(-10, 10), new BuildingSideInfo.NeighbourInfo[0]),
            new BuildingSideInfo(new Vector2(-10, 10), new Vector2(-10, -10), new BuildingSideInfo.NeighbourInfo[0]),
        };

        private static ScriptReference Finder(IEnumerable<KeyValuePair<string, string>> tags, params Type[] types)
        {
            Assert.IsNotNull(tags);
            return new ScriptReference(typeof(TestScript));
        }

        [TestMethod]
        public void AssertThat_BuildingFloorsAreAllUnique_WithBasicFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor { Tags: { 1: { a: b } } }
    - !Floor { Tags: { 1: { a: b } } }
    - !Ground []
"));

            var internals = b.Internals(new Random(1).NextDouble, null, Finder);

            Assert.AreEqual(2, internals.Floors.GroupBy(a => a.Index).Count());
        }

        [TestMethod]
        public void AssertThat_BuildingFloorsAreAllUnique_WithFloorRange()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Range
      Includes:
        - Count: 10
          Vary: false
          Tags:
            1: { a: b }
            1: { a: c }
            0: null
    - !Ground []
"));

            var internals = b.Internals(new Random(1).NextDouble, null, Finder);

            Assert.AreEqual(internals.Floors.Count(), internals.Floors.GroupBy(a => a.Index).Count());
        }

        [TestMethod]
        public void AssertThat_BuildingFloorsAreAllUnique_WithFloorepeat()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Aliases:
  - &residential_floor_count !NormalValue
    Min: 5
    Max: 10

Floors:
  - !Repeat
    Count:
      !NormalValue
      Min: 1
      Max: 5
    Items:
      - !Range
        Includes:
          - Count: *residential_floor_count
            Tags: { 1: { a: b } }
  - !Ground []
"));

            var internals = b.Internals(new Random(1).NextDouble, null, Finder);

            Assert.AreEqual(internals.Floors.Count(), internals.Floors.GroupBy(a => a.Index).Count());
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
        1: { a: b }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

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
        1: { a: b }
    - !Floor
      Height: *groupname
      Tags:
        1: { a: b }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

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
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

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
            1: { a: a }
            1: { a: b }
            0: null
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.IsTrue(selection.AboveGroundFloors.Any() && selection.AboveGroundFloors.Count() <= 5);
            Assert.AreEqual(0, selection.BelowGroundFloors.Count());
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacade()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: b } }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Top, Search: Up, Filter: Longest, NonOverlapping: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: Bot, Tags: { 1: { a: b } } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, _noNeighbours);

            //One facade for each wall (4 walls)
            Assert.AreEqual(4, selection.Walls.SelectMany(a => a.Facades).Count());

            //Every wall has just one facade up it's entire height
            Assert.IsTrue(selection.Walls.All(a => a.Facades.Count() == 1));
            Assert.IsTrue(selection.Walls.SelectMany(a => a.Facades).All(a => a.Bottom == 0 && a.Top == 4));
        }

        [TestMethod]
        [ExpectedException(typeof(DesignFailedException))]
        public void AssertThat_FacadeSelector_Throws_WhenNotAllFloorsAreCovered()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: b } }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: F, Search: Up, Filter: Shortest, NonOverlapping: true }    #Matching to the *first* F does not cover all floors!
Floors:
    - !Floor { Id: Top, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: Bot, Tags: { 1: { a: b } } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, _noNeighbours);
        }

        [TestMethod]
        public void AssertThat_FloorSelector_SelectsGroundMarkerAsFootprint()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: b } }
      Bottom: !Num { N: 0 }
      Top: !Id { Id: Top, Search: Up, Filter: Longest, NonOverlapping: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: Bot, Tags: { 1: { a: b } } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, _noNeighbours);

            //4 sides, 4 walls
            Assert.AreEqual(4, selection.Walls.Count());
            Assert.IsTrue(selection.Walls.All(a => a.BottomIndex == 0));

            //Does one of the walls contain one of the seed points
            Assert.AreEqual(1, selection.Walls.Count(a => a.Start == new Vector2(-10, -10)));
        }

        [TestMethod]
        public void AssertThat_FloorSelector_SelectsNextMarkerDownAsFootprint()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Floors:
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Footprint []
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.AreEqual(2, selection.Footprints.Count());
            Assert.IsTrue(selection.Footprints.Any(a => a.Index == 0));
            Assert.IsTrue(selection.Footprints.Any(a => a.Index == 2));
            Assert.IsTrue(selection.Footprints.Any(a => a.Marker is GroundMarker));
            Assert.IsTrue(selection.Footprints.Any(a => a.Marker is FootprintMarker));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacades()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: b } }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Inclusive: true, Search: Up, Filter: Longest, NonOverlapping: true }

    - Tags: { 1: { a: b } }
      Bottom: !Num { N: 0 }
      Top: !Num { N: 0, Inclusive: true }

    - Tags: { 1: { a: b } }
      Bottom: !Id { Id: Top }
      Top: !Id { Id: Top, Inclusive: true }
Floors:
    - !Floor { Id: Top, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: F, Tags: { 1: { a: b } } }
    - !Floor { Id: Bot, Tags: { 1: { a: b } } }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, _noNeighbours);

            var wall = selection.Walls.First();

            Assert.AreEqual(3, wall.Facades.Count());
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 0 && f.Top == 0));
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 1 && f.Top == 3));
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 4 && f.Top == 4));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacades_OnlyForNonObscuredFloors()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: a } }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Search: Up, Filter: Longest, NonOverlapping: true }
      Constraints: [ !Clearance { Distance: 20 } ]

    - Tags: { 1: { a: b } }
      Bottom: !Id { Id: '*' }
      Top: !Id { Id: '*', Inclusive: true, Filter: First }

Floors:
    - !Floor { Id: F, Tags: { 1: { a: c } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: d } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: e } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: f } }, Height: 1 }
    - !Ground []
"));

            Assert.IsNotNull(b);

            //Entire length obscured to 1m
            var n1 = new BuildingSideInfo.NeighbourInfo[] {
                new BuildingSideInfo.NeighbourInfo(0, 1, 1, new BuildingSideInfo.NeighbourInfo.Resource[0])
            };

            //Entire length of the first side is obscured up to height 1m (floor 1)
            var neighbours = new[] {
                new BuildingSideInfo(new Vector2(-10, -10), new Vector2(10, -10), n1),
                new BuildingSideInfo(new Vector2(10, -10), new Vector2(10, 10), n1),
                new BuildingSideInfo(new Vector2(10, 10), new Vector2(-10, 10), n1),
                new BuildingSideInfo(new Vector2(-10, 10), new Vector2(-10, -10), n1),
            };

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, neighbours);

            var wall = selection.Walls.First();

            //Without an obstacle we would get 1 facade, floor 0->3 (FFFF)
            //However, the ground floor is obscured, so we get 2 facades:
            //  F (blank) 0
            //  FFF (a) 1->3

            Assert.AreEqual(2, wall.Facades.Count());
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 1 && f.Top == 3));
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 0 && f.Top == 0));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_OutputsFacades_WhichDoNotViolateConstraints()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: a } }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Search: Up, Filter: Longest, NonOverlapping: true }
      Constraints: [ !Clearance { Distance: 20 } ]

    - Tags: { 1: { a: x } }
      Bottom: !Id { Id: '*' }
      Top: !Id { Id: '*', Inclusive: true, Filter: First }

Floors:
    - !Floor { Id: F, Tags: { 1: { a: b } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: c } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: d } }, Height: 1 }
    - !Ground []
"));

            Assert.IsNotNull(b);

            //Entire length obscured to 1m
            var n1 = new BuildingSideInfo.NeighbourInfo[] {
                new BuildingSideInfo.NeighbourInfo(0, 1, 1, new BuildingSideInfo.NeighbourInfo.Resource[0])
            };

            //Entire length of the first side is obscured up to height 1m (floor 1)
            var neighbours = new[] {
                new BuildingSideInfo(new Vector2(-10, -10), new Vector2(10, -10), n1),
                new BuildingSideInfo(new Vector2(10, -10), new Vector2(10, 10), n1),
                new BuildingSideInfo(new Vector2(10, 10), new Vector2(-10, 10), n1),
                new BuildingSideInfo(new Vector2(-10, 10), new Vector2(-10, -10), n1),
            };

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, neighbours);

            var wall = selection.Walls.First();

            //Without an obstacle we would get 1 facade floors 0->2
            //However floors zero is obscured by neighbours, so we should get a facade from 1->2 instead
            Assert.AreEqual(2, wall.Facades.Count());
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 1 && f.Top == 2));
            Assert.AreEqual(1, wall.Facades.Count(f => f.Bottom == 0 && f.Top == 0));
        }

        [TestMethod]
        public void AssertThat_FacadeSelector_DoesNotOutputFacadeAcrossFootprintBreak()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals: []
Facades:
    - Tags: { 1: { a: b } }
      Bottom: !Id { Id: F }
      Top: !Id { Id: F, Search: Up, Filter: Longest, NonOverlapping: true }

Floors:
    - !Floor { Id: F, Tags: { 1: { a: b } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: b } }, Height: 1 }
    - !Footprint []
    - !Floor { Id: F, Tags: { 1: { a: c } }, Height: 1 }
    - !Floor { Id: F, Tags: { 1: { a: d } }, Height: 1 }
    - !Ground []
"));

            Assert.IsNotNull(b);

            Random r = new Random(2);
            var selection = b.Internals(r.NextDouble, null, Finder).Externals(r.NextDouble, null, Finder, _noNeighbours);

            var facades = selection.Walls.SelectMany(a => a.Facades);

            //2 facades for all 4 walls
            Assert.AreEqual(2 * 4, facades.Count());

            //1 facade at this height for all 4 walls
            Assert.AreEqual(1 * 4, facades.Count(f => f.Bottom == 0 && f.Top == 1));
            Assert.AreEqual(1 * 4, facades.Count(f => f.Bottom == 2 && f.Top == 3));
        }

        [TestMethod]
        public void Playground()
        {
            var b = BuildingDesigner.Deserialize(new StringReader(@"
!Building
Verticals:
    - Tags: { 1: { tag: lift } }
      Bottom: !Id { Id: GroundFloor }
      Top: !Id { Id: TopFloor }
Floors:
    - !Floor { Id: TopFloor, Tags: { 1: { a: b }  } }
    - !Floor { Tags: { 1: { a: b}  } }
    - !Floor { Id: GroundFloor, Tags: { 1: { a: b }  } }
    - !Ground []
"));

            Random r = new Random();
            var selection = b.Internals(r.NextDouble, null, Finder);

            Assert.AreEqual(3, selection.AboveGroundFloors.Count());
            Assert.AreEqual(1, selection.Verticals.Count());
        }
    }

    [Script("93863CB4-2951-453E-95B8-955077322550", "Test")]
    internal class TestScript
    {
    }
}
