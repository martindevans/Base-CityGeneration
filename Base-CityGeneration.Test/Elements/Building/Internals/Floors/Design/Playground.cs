using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
//        [TestMethod]
//        public void TestMethod1()
//        {
//            var designer = FloorDesigner.Deserialize(new StringReader(@"
//# root node
//!Floorplan
//
//# tags for this floorplan
//Tags:
//  Type: Residential
//  Style: None
//  
//# Aliases block is just a list of items, not used by the system. Handy place to define objects which will be used later in the markup
//Aliases:
//    # A single room, with a set of constraints
//    - &lounge !Room
//      Id: Lounge
//      Tags:
//        1: { Type: Residential, Style: Modern, Class: LivingRoom }
//      # Constraints on the placement of the room
//      Constraints:
//         - { Strength: 1,    Req: !ExteriorWindow { } }
//#         - { Strength: 0.5,  Req: !ExteriorDoor { Deny: true } }
//         - { Strength: 0.5,  Req: !Area { Min: 13 } }
//      # Constraints on the connections between this room and other rooms
//      # Lounge can be used as a corridor to access other rooms
//      Walkthrough: true
//      Connections:
//        - { Strength: 0.75, Req: !IdRef { Id: Hallway } }
//        - { Strength: 0.5,  Req: !Either { A: !IdRef { Id: DiningRoom }, B: !RegexIdRef { Pattern: Kitchen }, Exclusive: false } }
//        - { Strength: 1,    Req: !Not { Inner: !IdRef { Id: Cloakroom } } }
//        - { Strength: 1,    Req: !Not { Inner: !IdRef { Id: Bathroom } } }
//
//    # Another room
//    - &kitchen !Room
//      Id: Kitchen
//      Tags:
//        1: { Type: Residential, Style: Modern, Class: Kitchen }
//      Constraints:
//        - { Strength: 1,    Req: !Area { Min: 8 } }
//        
//    # Another room
//    - &bedroom !Room
//      Id: Bedroom
//      Tags:
//        1: { Type: Residential, Style: Modern, Class: Bedroom }
//      Constraints:
//#        - { Strength: 1,  Req: !ExteriorDoor { Deny: true } }
//        - { Strength: 0.5,    Req: !Area { Min: 10 } }
//        
//    # Another room
//    - &bathroom !Room
//      Id: Bathroom
//      Tags:
//        1: { Type: Residential, Style: Modern, Class: Bathroom }
//      Constraints:
//#        - { Strength: 1,  Req: !ExteriorDoor { Deny: true } }
//        - { Strength: 0.5,    Req: !Area { Min: 4.5 } }
//
//    # A group is a group of rooms, it is laid out in exactly the same way as a single room, but itself contains further rooms (and thus has the union of all child rooms)
//    - &apartment !Group
//      Id: Apartment
//      Rooms:
//        - *lounge
//        - *kitchen
//        - *bedroom
//        - *bathroom
//      # This example has no constraints, but this is just to demo that a group can have constraints just like rooms
//      Constraints: []
//      Connections:
//        # This connection is to a hallway, and apartments contain hallways - this does not confuse the system! groups are laid out completely before the contents of each group
//        # i.e. this will look for a hallway at the same 'depth' as this group
//        - { Strength: 1, Req: !IdRef { Id: Hallway } }
//            
//    - &public_hallway !Room
//      Id: Hallway
//      Tags:
//        1: { Type: Residential, Style: Modern, Class: Corridor }
//      Walkthrough: false
//      Constraints: []
//      Connections: []
//      
//#Hallways:
//#    # If a hallway is closer than this to an external wall it will 'snap' to be adjacent to the wall
//#    SnapDistance: !UniformValue { Min: 3, Max: 5, Vary: true }
//#
//#    # If a hallway is farther than this from an external wall, it will generate an offset corridor  in between
//#    SplitDistance: !UniformValue { Min: 10, Max: 15 }
//
//Rooms:
//    - !Repeat
//      Required: 1
//      Optional: 1000
//      Room: *apartment
//"));

//            var rnd = new Random(2);
//            Func<double> random = rnd.NextDouble;
//            var meta = new NamedBoxCollection();

//            ////Octagon
//            //var floorplan = designer.Design(random, meta, null, new Vector2[] {
//            //    new Vector2(2,  4),
//            //    new Vector2(4,  2),
//            //    new Vector2(4,  -2),
//            //    new Vector2(2,  -4),
//            //    new Vector2(-2, -4),
//            //    new Vector2(-4, -2),
//            //    new Vector2(-4, 2),
//            //    new Vector2(-2, 4),
//            //});

//            //Corner shape
//            var floorplan = designer.Design(random, meta, null, new[] {
//                new FloorplanRegion.Side(new Vector2(6, 5), new Vector2(6, -6), new Section[]  { new Section(0, 1, Section.Types.Window) }),
//                new FloorplanRegion.Side(new Vector2(6, -6), new Vector2(0, -6), new Section[0]),
//                new FloorplanRegion.Side(new Vector2(0, -6), new Vector2(0, 0), new Section[0]),
//                new FloorplanRegion.Side(new Vector2(0, 0), new Vector2(-4, 0), new Section[0]),
//                new FloorplanRegion.Side(new Vector2(-4, 0), new Vector2(-4, 5), new Section[0]),
//                new FloorplanRegion.Side(new Vector2(-4, 5), new Vector2(6, 5), new Section[0]),
//            });

//            ////Weird spikey shape (unlikely to be generated)
//            //var floorplan = designer.Design(random, meta, null, new Vector2[] {
//            //    new Vector2(-10, 10),
//            //    new Vector2(-8, 10),
//            //    new Vector2(-6, 6),
//            //    new Vector2(-4, 10),
//            //    new Vector2(10, 10),
//            //    new Vector2(10, 3),
//            //    new Vector2(6, 0),
//            //    new Vector2(10, -3),
//            //    new Vector2(-10, -10),
//            //});

//            //Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(floorplan, 4).ToString());

//            Assert.IsTrue(true);
//        }

        [TestMethod]
        public void TestMethod1()
        {
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
      Constraints:
         - { Strength: 1,    Req: !ExteriorWindow { } }
#         - { Strength: 0.5,  Req: !ExteriorDoor { Deny: true } }
         - { Strength: 0.5,  Req: !Area { Min: 11 } }

    # Another room
    - &kitchen !Room
      Id: Kitchen
      Tags:
        1: { Kitchen }
      Constraints:
        - { Strength: 1,    Req: !Area { Min: 8 } }
        - { Strength: 0.25, Req: !ExteriorWindow { } }
      Connections:
        - { Strength: 1,    Req: !IdRef { Id: Lounge } }
        
    # Another room
    - &bedroom !Room
      Id: Bedroom
      Tags:
        1: { Bedroom }
      Constraints:
#        - { Strength: 1,  Req: !ExteriorDoor { Deny: true } }
        - { Strength: 0.5,    Req: !Area { Min: 10 } }
        - { Strength: 0.35,    Req: !ExteriorWindow { } }
      Connections:
        - { Strength: -1,    Req: !IdRef { Id: Bedroom } }
        
    # Another room
    - &bathroom !Room
      Id: Bathroom
      Tags:
        1: { Bathroom }
      Constraints:
#        - { Strength: 1,  Req: !ExteriorDoor { Deny: true } }
        - { Strength: 0.5,    Req: !Area { Min: 4.5 } }
        - { Strength: 0.15,    Req: !ExteriorWindow { } }
      Connections:
        - { Strength: -1,    Req: !IdRef { Id: Kitchen } }

    # A group of rooms
    - &apartment !Group
      Id: Apartment
      Rooms:
        - *lounge
        - *kitchen
        - *bathroom
        - *bedroom
        - *bedroom

Rooms:
    - !Repeat
      Required: 1
      Optional: 100
      Room: *apartment
"));

            var rnd = new Random(2);
            Func<double> random = rnd.NextDouble;
            var meta = new NamedBoxCollection();

            ////Octagon
            //var floorplan = designer.Design(random, meta, null, new Vector2[] {
            //    new Vector2(2,  4),
            //    new Vector2(4,  2),
            //    new Vector2(4,  -2),
            //    new Vector2(2,  -4),
            //    new Vector2(-2, -4),
            //    new Vector2(-4, -2),
            //    new Vector2(-4, 2),
            //    new Vector2(-2, 4),
            //});

            Func<IEnumerable<KeyValuePair<string, string>>, Type[], ScriptReference> finder = (tags, types) => {

                var tagsClean = from tag in tags
                                let k = string.IsNullOrEmpty(tag.Key)
                                let v = string.IsNullOrEmpty(tag.Value)
                                where !k || !v
                                select (!k && !v) ? (tag.Key + ":" + tag.Value) : (k ? tag.Value : tag.Key);

                return ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tagsClean));
            };
            //Corner shape
            var floorplan = designer.Design(random, meta, finder, new[] {
                new FloorplanRegion.Side(new Vector2(9, 5), new Vector2(9, -6), new Section[]  { new Section(0, 1, Section.Types.Window) }),
                new FloorplanRegion.Side(new Vector2(9, -6), new Vector2(0, -6), new Section[0]),
                new FloorplanRegion.Side(new Vector2(0, -6), new Vector2(0, 0), new Section[0]),
                new FloorplanRegion.Side(new Vector2(0, 0), new Vector2(-4, 0), new Section[0]),
                new FloorplanRegion.Side(new Vector2(-4, 0), new Vector2(-4, 5), new Section[0]),
                new FloorplanRegion.Side(new Vector2(-4, 5), new Vector2(9, 5), new Section[0]),
            }, 0.075f);

            Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(floorplan, 15));

            ////Weird spikey shape (unlikely to be generated)
            //var floorplan = designer.Design(random, meta, null, new Vector2[] {
            //    new Vector2(-10, 10),
            //    new Vector2(-8, 10),
            //    new Vector2(-6, 6),
            //    new Vector2(-4, 10),
            //    new Vector2(10, 10),
            //    new Vector2(10, 3),
            //    new Vector2(6, 0),
            //    new Vector2(10, -3),
            //    new Vector2(-10, -10),
            //});

            //Console.WriteLine(SvgRoomVisualiser.FloorplanToSvg(floorplan, 4).ToString());

            Assert.IsTrue(true);
        }
    }
}
