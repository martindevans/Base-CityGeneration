using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design;
using Base_CityGeneration.Test.Elements.Building.Design;
using Base_CityGeneration.TestHelpers;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Design
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            var rnd = new Random(3);
            var random = (Func<double>)rnd.NextDouble;
            var metadata = new NamedBoxCollection();

            var designer = FloorDesigner.Deserialize(new StringReader(@"
# root node
!Floorplan

# tags for this floorplan
Tags:
  Type: Residential
  Style: None
  
# Aliases block is just a list of items, not used by the system. Handy place to define objects which will be used later in the markup
Aliases:
    # A single room, with a set of constraints
    - &office !Room
      Id: Office
      Walkthrough: false
      Tags:
        1: { Office }
#      Constraints: []
#         - { Strength: 1,    Req: !ExteriorWindow { } }
#         - { Strength: 0.5,  Req: !ExteriorDoor { Deny: true } }
#         - { Strength: 0.5,  Req: !Area { Min: 11 } }

    # A group of rooms
    - &office_group !Group
      Id: Offices
      Children:
        - *office
        - *office

GrowthParameters:
    SeedSpacing: !NormalValue { Min: 2.5, Mean: 5, Max: 6.5, Vary: true }
    SeedChance: 0.75
    IntersectionContinuationChance: 0.3
    ParallelCheck:
        Length: 1
        Width: 3.5
        Angle: 15
MergeParameters:
    AngularDeviation:
        Weight: 0.4
        Threshold: 0.5
    Convexity:
        Weight: 0.3
        Threshold: 0.5
    Area:
        Weight: 0.3
        Threshold: 45
        Cutoff: 5
CorridorParameters:
    Width: 1

Spaces:
    - !Repeat
      Count: !NormalValue { Min: 1, Max: 100 }
      Space: *office_group
"));

            Func<IEnumerable<KeyValuePair<string, string>>, Type[], ScriptReference> finder = (tags, types) =>
            {

                var tagsClean = from tag in tags
                                let k = string.IsNullOrEmpty(tag.Key)
                                let v = string.IsNullOrEmpty(tag.Value)
                                where !k || !v
                                select (!k && !v) ? (tag.Key + ":" + tag.Value) : (k ? tag.Value : tag.Key);

                return ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tagsClean));
            };

            ////Corner shape
            //var shape = new[] {
            //    new Vector2(9, 5), new Vector2(9, -6), new Vector2(0, -6), new Vector2(0, 0), new Vector2(-4, 0), new Vector2(-4, 5)
            //};
            //var sections = new[] {
            //    new Subsection[] { new Subsection(0, 1, Subsection.Types.Window) },
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[] { new Subsection(0, 1, Subsection.Types.Window) },
            //    new Subsection[0]
            //};

            ////Diagonal bend shape
            //var shape = new[] {
            //    new Vector2(10, 10), new Vector2(20, 0), new Vector2(23, 0), new Vector2(33, 10), new Vector2(43, 0),
            //    new Vector2(28, -15), new Vector2(15, -15), new Vector2(0, 0)
            //};
            //var sections = new[] {
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0]
            //};
            //var verticals = new Vector2[0][] {

            //};

            //Actual office floorplan
            var shape = new[] {
                new Vector2(-25, 17),
                new Vector2(0, 17),
                new Vector2(3, 15),
                new Vector2(33, 15),
                new Vector2(38, 0),
                new Vector2(-25, -25)
            };
            var sections = new[] {
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0],
                new Subsection[0]
            };
            var verticals = new[] {
                new[] {
                    new Vector2(0, 0),
                    new Vector2(7, 0),
                    new Vector2(7, -7),
                    new Vector2(0, -7),
                }
            };

            ////rectangle
            //var shape = new[] {
            //    new Vector2(9, 0),
            //    new Vector2(9, -6),
            //    new Vector2(0, -6),
            //    new Vector2(0, 0)
            //};
            //var sections = new[] {
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //    new Subsection[0],
            //};

            var floorplan = designer.Design(random, metadata, finder, shape, sections, 0.175f, verticals, new List<VerticalSelection>());

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(floorplan, 55, basic:true));
        }
    }
}
