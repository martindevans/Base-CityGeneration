﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design;
using Base_CityGeneration.TestHelpers;
using Base_CityGeneration.Utilities.SVG;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
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
      Tags:
        1: { Type: Residential, Style: Modern, Class: LivingRoom }
      # Constraints on the placement of the room
      Constraints:
         - { Strength: 1,    Req: !Exterior { Window: true } }
         - { Strength: 0.5,  Req: !Exterior { Door: false } }
      # Constraints on the connections between this room and other rooms
      # Lounge can be used as a corridor to access other rooms
      Walkthrough: true
      Connections:
        - { Strength: 1,    Req: !IdRef { Id: Hallway } }
        - { Strength: 0.5,  Req: !Either { A: !IdRef { Id: DiningRoom }, B: !RegexIdRef { Pattern: Kitchen }, Exclusive: false } }
        - { Strength: -1,   Req: !IdRef { Id: Cloakroom } }
        - { Strength: -1,   Req: !IdRef { Id: Bathroom } }

    # Another room
    - &kitchen !Room
      Id: Kitchen
      Tags:
        1: { Type: Residential, Style: Modern, Class: Kitchen }
      Constraints:
        - { Strength: 1,    Req: !Area { Min: 9 } }

    # A group is a group of rooms, it is laid out in exactly the same way as a single room, but itself contains further rooms (and thus has the union of all child rooms)
    - &apartment !Group
      Id: Apartment
      Rooms:
        - *lounge
      # This example has no constraints, but this is just to demo that a group can have constraints just like rooms
      Constraints: []
      Connections:
        # This connection is to a hallway, and apartments contain hallways - this does not confuse the system! groups are laid out completely before the contents of each group
        # i.e. this will look for a hallway at the same 'depth' as this group
        - { Strength: 1, Req: !IdRef { Id: Hallway } }
            
    - &public_hallway !Room
      Id: Hallway
      Tags:
        1: { Type: Residential, Style: Modern, Class: Corridor }
      Walkthrough: false
      Constraints: []
      Connections: []
      
#Hallways:
#    # If a hallway is closer than this to an external wall it will 'snap' to be adjacent to the wall
#    SnapDistance: !UniformValue { Min: 3, Max: 5, Vary: true }
#
#    # If a hallway is farther than this from an external wall, it will generate an offset corridor  in between
#    SplitDistance: !UniformValue { Min: 10, Max: 15 }

Rooms:
    - *apartment
    - !Repeat
      Required: !UniformValue { Min: 1, Max: 3 }
      Optional: !UniformValue { Min: 1, Max: 3 }
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

            //Corner shape
            var floorplan = designer.Design(random, meta, null, new Vector2[] {
                new Vector2(5,  5),
                new Vector2(5,  -6),
                new Vector2(0,  -6),
                new Vector2(0,  0),
                new Vector2(-4, 0),
                new Vector2(-4, 5),
            });

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

//        [TestMethod]
//        public void TestMethod2()
//        {
//            FloorDesigner.Deserialize(new StringReader(@"
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
//        Require:
//            - !Exterior { Window: true }
//        Prefer:
//            - !Exterior { Door: false }
//      # Constraints on the connections between this room and other rooms
//      Connections:
//        # Lounge can be used as a corridor to access other rooms
//        Walkthrough: true
//        Require:
//            - { Id: Hallway }
//            - !Either { A: { Id: DiningRoom }, B: { Id: Kitchen }, Exclusive: false }
//        Exclude:
//            - { Id: Cloakroom }
//            - { Id: Bathroom }
//
//    # A group is a group of rooms, it is laid out in exactly the same way as a single room, but itself contains further rooms (and thus has the union of all child rooms)
//    - &apartment !Group
//      Id: Apartment
//      Rooms:
//        - *lounge
//      # This example has no constraints, but this is just to demo that a group can have constraints just like rooms
//      Constraints: []
//      Connections:
//        Require:
//            # This connection is to a hallway, and apartments contain hallways - this does not confuse the system! groups are laid out completely before the contents of each group
//            # i.e. this will look for a hallway at the same 'depth' as this group
//            - { Id: Hallway }
//            
//    - &public_hallway !Room
//      Id: Hallway
//      Tags:
//        1: { Type: Residential, Style: Modern, Class: Corridor }
//      Constraints:
//        - ???
//      Connections:
//      Walkthrough: false
//        - ???
//      
//Rooms:
//    - 
//"));
//        }

        [TestMethod]
        public void MethodName()
        {
            Random r = new Random(1);
            Func<double> rand = r.NextDouble;

            var points = new List<Vector2>();
            for (int i = 0; i < 100; i++)
                points.Add(new Vector2(rand.NormallyDistributedSingle(25, 0), rand.NormallyDistributedSingle(25, 0)));

            var hull = points.ConvexHull().ToArray();

            var oabr = OABR.Fit(hull);

            var svg = new SvgRenderer(2);
            svg.AddOutline(hull, "red");
            svg.AddOutline((IReadOnlyList<Vector2>)oabr.Points(new Vector2[4]));
            Console.WriteLine(svg.Render());
        }
    }
}
