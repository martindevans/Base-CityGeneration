using System.IO;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Design
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            FloorDesigner.Deserialize(new StringReader(@"
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
      Constraints: []
# - { Strength: 1,    Req: !Exterior { Window: true } }
# - { Strength: 0.5,  Req: !Exterior { Door: false } }
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
      
Rooms:
    - *apartment
#- !Repeat
#     Count: !UniformValue { Min: 1, Max: 10 }
#     Room: *apartment
"));
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
    }
}
