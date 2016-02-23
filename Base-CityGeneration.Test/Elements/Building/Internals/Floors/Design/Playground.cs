using System;
using System.IO;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design;
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
            var rnd = new Random(2);
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
    - &lounge !Room
      Id: Lounge
      Walkthrough: true
      Tags:
        1: { LivingRoom }
#      Constraints: []
#         - { Strength: 1,    Req: !ExteriorWindow { } }
#         - { Strength: 0.5,  Req: !ExteriorDoor { Deny: true } }
#         - { Strength: 0.5,  Req: !Area { Min: 11 } }

    # An apartment
    - &apartment !Group
      Id: Apartment
      Children:
        - *lounge
        - *lounge

Spaces:
    - !Repeat
      Count: !NormalValue { Min: 1, Max: 100 }
      Space: *apartment
"));

            //////Octagon
            ////var floorplan = designer.Design(random, meta, null, new Vector2[] {
            ////    new Vector2(2,  4),
            ////    new Vector2(4,  2),
            ////    new Vector2(4,  -2),
            ////    new Vector2(2,  -4),
            ////    new Vector2(-2, -4),
            ////    new Vector2(-4, -2),
            ////    new Vector2(-4, 2),
            ////    new Vector2(-2, 4),
            ////});

            //Func<IEnumerable<KeyValuePair<string, string>>, Type[], ScriptReference> finder = (tags, types) =>
            //{

            //    var tagsClean = from tag in tags
            //                    let k = string.IsNullOrEmpty(tag.Key)
            //                    let v = string.IsNullOrEmpty(tag.Value)
            //                    where !k || !v
            //                    select (!k && !v) ? (tag.Key + ":" + tag.Value) : (k ? tag.Value : tag.Key);

            //    return ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tagsClean));
            //};

            ////Corner shape
            //var floorplan = designer.Design(random, meta, finder, new[] {
            //    new FloorplanRegion.Side(new Vector2(9, 5), new Vector2(9, -6), new Section[] { new Section(0, 1, Section.Types.Window) }),
            //    new FloorplanRegion.Side(new Vector2(9, -6), new Vector2(0, -6), new Section[0]),
            //    new FloorplanRegion.Side(new Vector2(0, -6), new Vector2(0, 0), new Section[0]),
            //    new FloorplanRegion.Side(new Vector2(0, 0), new Vector2(-4, 0), new Section[0]),
            //    new FloorplanRegion.Side(new Vector2(-4, 0), new Vector2(-4, 5), new Section[] { new Section(0, 1, Section.Types.Window) }),
            //    new FloorplanRegion.Side(new Vector2(-4, 5), new Vector2(9, 5), new Section[0]),
            //}, 0.075f, new List<IReadOnlyList<Vector2>>(), new List<VerticalSelection>());

            ////var floorplan = designer.Design(random, meta, finder, new[] {
            ////    new FloorplanRegion.Side(new Vector2(-25, 17), new Vector2(0, 17), new Section[0]),
            ////    new FloorplanRegion.Side(new Vector2(0, 17), new Vector2(3, 15), new Section[] { new Section(0, 1, Section.Types.Window) }),
            ////    new FloorplanRegion.Side(new Vector2(3, 15), new Vector2(33, 15), new Section[] { new Section(0, 1, Section.Types.Window) }),
            ////    new FloorplanRegion.Side(new Vector2(33, 15), new Vector2(38, 0), new Section[0]),
            ////    new FloorplanRegion.Side(new Vector2(38, 0), new Vector2(-25, -25), new Section[] { new Section(0, 1, Section.Types.Window) }),
            ////    new FloorplanRegion.Side(new Vector2(-25, -25), new Vector2(-25, 17), new Section[] { new Section(0, 1, Section.Types.Window) }),
            ////}, 0.075f, new List<IReadOnlyList<Vector2>>(), new List<VerticalSelection>());

            //////simple rectangle shape
            ////var floorplan = designer.Design(random, meta, finder, new[] {
            ////    new FloorplanRegion.Side(new Vector2(9, 0), new Vector2(9, -6), new Section[]  { new Section(0, 1, Section.Types.Window) }),
            ////    new FloorplanRegion.Side(new Vector2(9, -6), new Vector2(0, -6), new Section[0]),
            ////    new FloorplanRegion.Side(new Vector2(0, -6), new Vector2(0, 0), new Section[0]),
            ////    new FloorplanRegion.Side(new Vector2(0, 0), new Vector2(9, 0), new Section[0]),
            ////}, 0.075f);

            //Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(floorplan, 15));

            //////Weird spikey shape (unlikely to be generated)
            ////var floorplan = designer.Design(random, meta, null, new Vector2[] {
            ////    new Vector2(-10, 10),
            ////    new Vector2(-8, 10),
            ////    new Vector2(-6, 6),
            ////    new Vector2(-4, 10),
            ////    new Vector2(10, 10),
            ////    new Vector2(10, 3),
            ////    new Vector2(6, 0),
            ////    new Vector2(10, -3),
            ////    new Vector2(-10, -10),
            ////});

            ////Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(floorplan, 4).ToString());

            //Assert.IsTrue(true);
        }
    }
}
