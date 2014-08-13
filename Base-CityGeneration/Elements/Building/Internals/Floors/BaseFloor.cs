using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    /// <summary>
    /// A floor placed into a section of empty space.
    /// </summary>
    public abstract class BaseFloor
        :ProceduralScript, IFloor, IFacadeProvider
    {
        private readonly float _minHeight;
        private readonly float _maxHeight;
        private readonly float _floorThickness;
        private readonly float _ceilingThickness;

        public FloorPlan Plan { get; private set; }

        /// <summary>
        /// The index of this floor
        /// </summary>
        public int FloorIndex { get; set; }

        public IVerticalFeature[] Overlaps { get; set; }

        protected BaseFloor(float minHeight = 1.5f, float maxHeight = 4, float floorThickness = 0.1f, float ceilingThickness = 0.1f)
        {
            _minHeight = minHeight;
            _maxHeight = maxHeight;
            _floorThickness = floorThickness;
            _ceilingThickness = ceilingThickness;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return bounds.Height >= _minHeight
                && bounds.Height <= _maxHeight;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            ////Create rooms for all the vertical overlaps
            //foreach (var overlap in Overlaps)
            //{
            //    var points = overlap.Bounds.Footprint.ToArray();
            //    var w = overlap.WorldTransformation * InverseWorldTransformation;
            //    Vector2.Transform(points, ref w, points);
            //    var r = Plan.AddRoom(points, 0.1f, ScriptReference.Find<IdentityScript>(), false);
            //    if (r.Count() != 1)
            //        throw new InvalidOperationException("Adding Vertical element failed");
            //    r.Single().Tag = overlap;

            //    brush = brush.Subtract(geometry.CreatePrism(overlap.HierarchicalParameters.GetValue(new TypedName<string>("material")), points, bounds.Height));
            //}

            var externalWallThickness = hierarchicalParameters.GetMaybeValue(new TypedName<float>("external_wall_thickness")) ?? 0.25f;
            var material = hierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Create rooms
            Plan = new FloorPlan(bounds.Footprint, externalWallThickness);
            CreateRooms(Plan);
            Plan.Freeze();

            //Brush to fill the entire floor (except external walls)
            ICsgShape brush = 
                geometry.CreateEmpty();
                //geometry.CreatePrism(material, bounds.Footprint, bounds.Height);

            var roomHeight = bounds.Height - _floorThickness - _ceilingThickness;
            var roomOffsetY = -bounds.Height / 2 + roomHeight / 2 + _floorThickness;

            ////Cut rooms out of brush
            //foreach (var roomInfo in Plan.Rooms)
            //{
            //    var rmBrush = geometry.CreatePrism(material, roomInfo.InnerFootprint, roomHeight)
            //            .Translate(new Vector3(0, roomOffsetY, 0));
            //    brush = brush.Subtract(rmBrush);
            //}

            //Union rooms into brush
            foreach (var roomInfo in Plan.Rooms)
            {
                foreach (var section in roomInfo.GetFacades())
                {
                    if (section.Section.IsCorner && section.IsExternal)
                    {
                        geometry.Union(geometry.CreatePrism(material, new[]
                        {
                            section.Section.A, section.Section.B, section.Section.C, section.Section.D
                        }, roomHeight).Translate(new Vector3(0, roomOffsetY, 0)));
                    }
                }
            }

            ////Create room scripts (and set them on the tag)
            //foreach (var roomInfo in Plan.Rooms)
            //{
            //    var room = (IRoom)CreateChild(new Prism(roomHeight, roomInfo.InnerFootprint), Quaternion.Identity, new Vector3(0, roomOffsetY, 0), roomInfo.Scripts);
            //    roomInfo.Node = room;
            //}

            ////Create the four corner sections of this floor
            ////These corner pillars can't possibly be assigned to a room
            //var sections = bounds.Footprint.Sections(bounds.Footprint.Shrink(externalWallThickness)).ToArray();
            //foreach (var section in sections.Where(s => s.IsCorner))
            //{
            //    var c = geometry.CreatePrism(material, new Vector2[]
            //    {
            //        section.A, section.B, section.C, section.D
            //    }, bounds.Height);

            //    brush = brush.Union(c);
            //}

            ////Create facades for rooms
            ////There are three types of facade:
            //// 1. A border between 2 rooms
            ////  - Create a NegotiableFacade between rooms and store for later retrieval
            //// 2. A facade onto nothing (dead space behind facade)
            ////  - Do nothing - room can create this facade itself
            //// 3. An external wall
            ////  - Create an external facade and then wrap subsections of wall using a SubsectionFacade
            //foreach (var roomInfo in Plan.Rooms)
            //{
            //    FloorPlan.RoomInfo info = roomInfo;
            //    var neighbours = Plan.GetNeighbours(roomInfo).Where(a => a.Other(info).Id < info.Id).ToArray();
            //}

            //Union floor into world
            geometry.Union(brush);
        }

        /// <summary>
        /// Call Plan.AddRoom to put new rooms into the floor plan
        /// </summary>
        /// <param name="plan"></param>
        protected abstract void CreateRooms(FloorPlan plan);

        #region IFacadeProvider implementation
        public IFacade CreateFacade(IFacadeOwner owner, Walls.Section section)
        {
            //If section is a subsection of an external wall return a SubsectionFacade
            //If section is 

            if (!(owner is IRoom))
                return null;

            var room = (IRoom)owner;

            //Plan.GetNeighbours(room);

            return null;
        }

        public void ConfigureFacade(IFacade facade, IFacadeOwner owner)
        {
            facade.AddPrerequisite(owner);
        }
        #endregion
    }
}
