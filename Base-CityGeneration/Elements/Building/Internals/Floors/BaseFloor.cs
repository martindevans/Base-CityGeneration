﻿using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Generic;
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
        :ProceduralScript, IFloor, IRoomPlanProvider
    {
        private readonly float _minHeight;
        private readonly float _maxHeight;
        private readonly float _floorThickness;
        private readonly float _ceilingThickness;

        private float _roomHeight;
        private float _roomOffsetY;

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
            //Get some style parameters
            var externalWallThickness = hierarchicalParameters.GetMaybeValue(new TypedName<float>("external_wall_thickness")) ?? 0.25f;
            var material = hierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Calculate some handy values
            _roomHeight = bounds.Height - _floorThickness - _ceilingThickness;
            _roomOffsetY = -bounds.Height / 2 + _roomHeight / 2 + _floorThickness;

            //Create rooms for vertical features which overlap this floor
            var verticalSubsections = CreateVerticalOverlapRooms();

            //Create other rooms in the plan
            CreateFloorPlan(bounds);

            //Create Floor and ceiling (with holes for vertical sections)
            CreateFloors(bounds, geometry, verticalSubsections, null);
            CreateCeilings(bounds, geometry, verticalSubsections, null);

            //Create room scripts
            CreateRoomScripts();

            //Create external facades
            var externalFacades = CreateExternalFacades(bounds, externalWallThickness);

            //Create facades for rooms
            CreateRoomFacades(externalFacades);
        }

        #region helpers
        private Dictionary<RoomPlan, IVerticalFeature> CreateVerticalOverlapRooms()
        {
            Dictionary<RoomPlan, IVerticalFeature> verticalSubsections = (Overlaps ?? new IVerticalFeature[0]).Select(overlap =>
            {
                //Transform overlap into coordinate frame of floor
                var points = overlap.Bounds.Footprint.ToArray();
                var w = overlap.WorldTransformation * InverseWorldTransformation;
                Vector2.Transform(points, ref w, points);

                //Create a room using the identity script (todo: change using identity script for vertical features?)
                var r = Plan.AddRoom(points, 0.1f, new[] { new ScriptReference(typeof(IdentityScript)) }, false).Single();
                return new KeyValuePair<RoomPlan, IVerticalFeature>(r, overlap);
            }).ToDictionary(a => a.Key, a => a.Value);
            return verticalSubsections;
        }

        private void CreateFloorPlan(Prism bounds)
        {
            Plan = new FloorPlan(bounds.Footprint);
            CreateRooms(Plan);
            Plan.Freeze();
        }

        private void CreateFloors(Prism bounds, ISubdivisionGeometry geometry, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections, string material)
        {
            geometry.Union(geometry.CreatePrism(material, bounds.Footprint, _floorThickness).Translate(new Vector3(0, -bounds.Height / 2 + _floorThickness / 2, 0)));
        }

        private void CreateCeilings(Prism bounds, ISubdivisionGeometry geometry, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections, string material)
        {
            geometry.Union(geometry.CreatePrism(material, bounds.Footprint, _ceilingThickness).Translate(new Vector3(0, bounds.Height / 2 - _ceilingThickness / 2, 0)));
        }

        private void CreateRoomScripts()
        {
            foreach (var roomPlan in Plan.Rooms)
            {
                var room = (IPlannedRoom)CreateChild(
                    new Prism(_roomHeight, roomPlan.InnerFootprint),
                    Quaternion.Identity,
                    new Vector3(0, _roomOffsetY, 0), roomPlan.Scripts.Where(r => r.Implements<IPlannedRoom>())
                );
                if (room != null)
                    roomPlan.Node = room;
            }
        }

        private List<IConfigurableFacade> CreateExternalFacades(Prism bounds, float externalWallThickness)
        {
            var externalSections = new List<IConfigurableFacade>();

            foreach (var section in Plan.ExternalFootprint.Sections(externalWallThickness))
            {
                var facade = CreateChild(new Prism(bounds.Height, section.A, section.B, section.C, section.D), Quaternion.Identity, Vector3.Zero,
                    ExternalFacadeScripts().Where(e => e.Implements<IConfigurableFacade>())
                );

                if (facade != null)
                {
                    facade.HierarchicalParameters.Set(new TypedName<string>("material"), "concrete");

                    var c = (IConfigurableFacade)facade;
                    c.Section = section;
                    externalSections.Add(c);
                }
            }

            return externalSections;
        }

        private void CreateRoomFacades(List<IConfigurableFacade> externalFacades)
        {
            //There are three types of facade:
            // 1. A border between 2 rooms
            //  - Create a NegotiableFacade between rooms and store for later retrieval
            // 2. A facade onto nothing (dead space behind facade)
            //  - Create an IConfigurableFacade, pass it to the room
            // 3. An external wall
            //  - Get relevant external facade and then wrap subsection of it

            foreach (var roomPlan in Plan.Rooms.Where(r => r.Node != null).OrderBy(r => r.Id))
            {
                Dictionary<RoomPlan.Facade, IConfigurableFacade> generatedFacades = new Dictionary<RoomPlan.Facade, IConfigurableFacade>();

                var facades = roomPlan.GetFacades();
                foreach (var facade in facades)
                {
                    if (facade.IsExternal)
                    {
                        //Find the external wall which is co-linear with this facade section
                        var externalSection = externalFacades.FirstOrDefault(e =>
                        {
                            Geometry2D.Parallelism parallelism;
                            Geometry2D.LineLineIntersection(e.Section.ExternalLineSegment.Line(), facade.Section.ExternalLineSegment.Line(), out parallelism);
                            return parallelism == Geometry2D.Parallelism.Collinear;
                        });

                        //Create section (or call error handler if no externals ection was found)
                        var f = externalSection == null ? FailedToFindExternalSection(roomPlan, facade) : CreateExternalWall(roomPlan, facade, externalSection);
                        if (f != null)
                            generatedFacades.Add(facade, f);
                    }
                    else if (facade.NeighbouringRoom != null && facade.NeighbouringRoom.Node != null)
                    {
                        if (roomPlan.Id < facade.NeighbouringRoom.Id)
                            throw new NotImplementedException("Create facade between these two rooms");
                        else
                            throw new NotImplementedException("Find already created facade between these two rooms");
                    }
                    else
                    {
                        var f = CreateInternalWall(roomPlan, facade);
                        if (f != null)
                            generatedFacades.Add(facade, f);
                    }
                }

                roomPlan.Node.Facades = generatedFacades;
                foreach (var facade in generatedFacades.Values)
                {
                    var context = facade as ISubdivisionContext;
                    if (context != null)
                        context.AddPrerequisite(roomPlan.Node);
                }
            }
        }

        /// <summary>
        /// External wall generation failed to find a setion which is co-linear with the given facade section. Override this method to handle this problem in a different way (by default do nothing)
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <param name="facade"></param>
        protected virtual IConfigurableFacade FailedToFindExternalSection(RoomPlan roomPlan, RoomPlan.Facade facade)
        {
            return null;
        }

        protected virtual IConfigurableFacade CreateExternalWall(RoomPlan roomPlan, RoomPlan.Facade facade, IConfigurableFacade externalSection)
        {
            //Make sure the room subdivides before the facade (and thus has a chance to configure it
            ((ISubdivisionContext) externalSection).AddPrerequisite(roomPlan.Node);
            
            //Calculate X position of subsection (map room section onto full wall section)
            var at = Geometry2D.ClosestPointDistanceAlongLine(externalSection.Section.InternalLineSegment.LongLine(), facade.Section.ExternalLineSegment.Start);
            var bt = Geometry2D.ClosestPointDistanceAlongLine(externalSection.Section.InternalLineSegment.LongLine(), facade.Section.ExternalLineSegment.End);

            //Transform distance along facade into facade local coordinates
            var minAlong = Math.Min(at, bt) * externalSection.Section.Width - externalSection.Section.Width * 0.5f;
            var maxAlong = Math.Max(at, bt) * externalSection.Section.Width - externalSection.Section.Width * 0.5f;

            return new SubsectionFacade(
                externalSection,
                new Vector2(minAlong, -Bounds.Height / 2 + _floorThickness),
                new Vector2(maxAlong, -Bounds.Height / 2 + _floorThickness + _roomHeight),
                0, 1,
                facade.Section
            );
        }

        protected virtual IConfigurableFacade CreateInternalWall(RoomPlan room, RoomPlan.Facade facade)
        {
            var wall = (IConfigurableFacade)CreateChild(
                new Prism(_roomHeight, facade.Section.A, facade.Section.B, facade.Section.C, facade.Section.D),
                Quaternion.Identity,
                new Vector3(0, _roomOffsetY, 0),
                InternalFacadeScripts(room).Where(r => r.Implements<IConfigurableFacade>())
            );

            wall.Section = facade.Section;

            //Make sure the room subdivides before the facade (and thus has a chance to configure it
            ((ISubdivisionContext)wall).AddPrerequisite(room.Node);

            return wall;
        }

        /// <summary>
        /// Return a set of possible scripts to use for the external facades. Must implement IConfigurableFacade
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> ExternalFacadeScripts()
        {
            return ScriptReference.Find<IConfigurableFacade>();
        }

        /// <summary>
        /// Return a set of possible scripts to use for internal facades. Must implement IConfigurableFacade
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> InternalFacadeScripts(RoomPlan roomPlan)
        {
            return ScriptReference.Find<IConfigurableFacade>();
        }

        /// <summary>
        /// Return a set of possible scripts to use for internal (neighbour) facades. Must implement IConfigurableFacade
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> InternalNeighbourFacadeScripts(RoomPlan roomPlan)
        {
            return ScriptReference.Find<IConfigurableFacade>();
        }
        #endregion

        #region abstracts
        /// <summary>
        /// Call Plan.AddRoom to put new rooms into the floor plan
        /// </summary>
        /// <param name="plan"></param>
        protected abstract void CreateRooms(FloorPlan plan);
        #endregion

        #region IRoomPlanProvider implementation
        public RoomPlan GetPlan(IPlannedRoom room)
        {
            return Plan.Rooms.SingleOrDefault(r => ReferenceEquals(r.Node, room));
        }
        #endregion
    }
}
