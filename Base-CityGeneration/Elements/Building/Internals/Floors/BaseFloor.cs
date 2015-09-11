﻿using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using Myre.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors;

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

        public FloorPlan Plan { get; private set; }

        /// <summary>
        /// The index of this floor
        /// </summary>
        public int FloorIndex { get; set; }

        public IReadOnlyCollection<IVerticalFeature> Overlaps { get; set; }

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
            //Calculate some handy values
            _roomHeight = bounds.Height - _floorThickness - _ceilingThickness;
            var roomOffsetY = -bounds.Height / 2 + _roomHeight / 2 + _floorThickness;

            //Create an empty floor plan
            Plan = new FloorPlan(Bounds.Footprint);

            //Create rooms for vertical features which overlap this floor
            var verticalSubsections = CreateVerticalOverlapRooms(Plan);

            //Create other rooms in the plan
            CreateFloorPlan(bounds, Plan);

            //Create Floor and ceiling (with holes for vertical sections)
            CreateFloors(bounds, geometry, verticalSubsections, null);
            CreateCeilings(bounds, geometry, verticalSubsections, null);

            //todo: rooms need information on external facades, such that they can ensure rooms are not created splitting windows

            //Create room scripts
            CreateRoomScripts(roomOffsetY, _roomHeight, Plan);

            //Rooms have been created
            CreatedRooms(Plan);

            //Create external facades (subsections of building over this floor facade)
            var externalFacades = CreateExternalFacades(bounds);

            //Create facades for rooms
            CreateRoomFacades(externalFacades, roomOffsetY, Plan);
        }

        #region helpers
        private Dictionary<RoomPlan, IVerticalFeature> CreateVerticalOverlapRooms(FloorPlan plan)
        {
            Dictionary<RoomPlan, IVerticalFeature> verticalSubsections = (Overlaps ?? new IVerticalFeature[0]).Select(overlap =>
            {
                //Transform overlap into coordinate frame of floor
                var points = overlap.Bounds.Footprint.ToArray();
                var w = overlap.WorldTransformation * InverseWorldTransformation;
                for (int i = 0; i < points.Length; i++)
                    points[i] = Vector3.Transform(points[i].X_Y(0), w).XZ();

                //Clockwise wind
                if (points.Area() < 0)
                    Array.Reverse(points);

                //Create a room using the identity script (todo: change using identity script for vertical features?)
                var r = plan.AddRoom(points, 0.1f, new[] { new ScriptReference(typeof(IdentityScript)) }).Single();
                return new KeyValuePair<RoomPlan, IVerticalFeature>(r, overlap);
            }).ToDictionary(a => a.Key, a => a.Value);
            return verticalSubsections;
        }

        private void CreateFloorPlan(Prism bounds, FloorPlan plan)
        {
            CreateRooms(plan);
            plan.Freeze();
        }

        private void CreateFloors(Prism bounds, ISubdivisionGeometry geometry, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections, string material)
        {
            var floor = geometry.CreatePrism(material, bounds.Footprint, _floorThickness).Translate(new Vector3(0, -bounds.Height / 2 + _floorThickness / 2, 0));

            floor = CutVerticalHoles(floor, geometry, material, verticalSubsections);

            geometry.Union(floor);
        }

        private void CreateCeilings(Prism bounds, ISubdivisionGeometry geometry, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections, string material)
        {
            var ceiling = geometry.CreatePrism(material, bounds.Footprint, _ceilingThickness).Translate(new Vector3(0, bounds.Height / 2 - _ceilingThickness / 2, 0));

            ceiling = CutVerticalHoles(ceiling, geometry, material, verticalSubsections);

            geometry.Union(ceiling);
        }

        private ICsgShape CutVerticalHoles(ICsgShape shape, ISubdivisionGeometry geometry, string material, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections)
        {
            foreach (var verticalSubsection in verticalSubsections)
            {
                shape = shape.Subtract(
                    geometry.CreatePrism(material, verticalSubsection.Key.OuterFootprint, 1000)
                );
            }

            return shape;
        }

        private void CreateRoomScripts(float yOffset, float height, FloorPlan plan)
        {
            foreach (var roomPlan in plan.Rooms)
            {
                var room = (IPlannedRoom)CreateChild(
                    new Prism(height, roomPlan.InnerFootprint),
                    Quaternion.Identity,
                    new Vector3(0, yOffset, 0), roomPlan.Scripts.Where(r => r.Implements<IPlannedRoom>())
                );
                if (room != null)
                    roomPlan.Node = room;
            }
        }

        protected virtual void CreatedRooms(FloorPlan plan)
        {
        }

        private List<IConfigurableFacade> CreateExternalFacades(Prism bounds)
        {
            //var externalSections = new List<IConfigurableFacade>();

            //foreach (var section in Plan.ExternalFootprint.Sections(externalWallThickness))
            //{
            //    var facade = CreateChild(new Prism(bounds.Height, section.A, section.B, section.C, section.D), Quaternion.Identity, Vector3.Zero,
            //        ExternalFacadeScripts().Where(e => e.Implements<IConfigurableFacade>())
            //    );

            //    if (facade != null)
            //    {
            //        facade.HierarchicalParameters.Set(new TypedName<string>("material"), "concrete");

            //        var c = (IConfigurableFacade)facade;
            //        c.Section = section;
            //        externalSections.Add(c);
            //    }
            //}

            //return externalSections;

            //todo: create subsection of appropriate building facade
            return new List<IConfigurableFacade>();
        }

        private void CreateRoomFacades(IReadOnlyCollection<IConfigurableFacade> externalFacades, float yOffset, FloorPlan plan)
        {
            //There are three types of facade:
            // 1. A border between 2 rooms
            //  - Create a NegotiableFacade between rooms and store for later retrieval
            //  - Find an existing NegotiableFacade between two rooms, and return later (but wrapped in an adapter which reverses access from one side to the other)
            // 2. A facade onto nothing (dead space behind facade)
            //  - Create an IConfigurableFacade, pass it to the room
            // 3. An external wall
            //  - Get relevant external facade and then wrap subsection of it

            // Facades between rooms
            // Key is both rooms (in ID order), value is the all the facades between the two rooms
            Dictionary<KeyValuePair<RoomPlan, RoomPlan>, List<IConfigurableFacade>> interRoomFacades = new Dictionary<KeyValuePair<RoomPlan, RoomPlan>, List<IConfigurableFacade>>();

            foreach (var roomPlan in plan.Rooms.Where(r => r.Node != null).OrderBy(r => r.Id))
            {
                //All facades generated for this room
                Dictionary<RoomPlan.Facade, IConfigurableFacade> generatedFacades = new Dictionary<RoomPlan.Facade, IConfigurableFacade>();

                var facades = roomPlan.GetFacades();

                foreach (var facade in facades)
                {
                    IConfigurableFacade newFacade;

                    if (facade.IsExternal)
                    {
                        //Find the external wall which is co-linear with this facade section
                        var externalSection = FindExternalFacade(roomPlan.WallThickness, externalFacades, facade.Section.ExternalLineSegment); 

                        //Create section (or call error handler if no external section was found)
                        newFacade = externalSection == null ? FailedToFindExternalSection(roomPlan, facade) : CreateExternalWall(roomPlan, facade, externalSection);
                    }
                    else if (facade.NeighbouringRoom != null && facade.NeighbouringRoom.Node != null)
                    {
                        if (roomPlan.Id < facade.NeighbouringRoom.Id)
                        {
                            //Create a new facade between these rooms and store it for the other room to retrieve later
                            newFacade = CreateInternalWall(roomPlan, facade, yOffset);
                            interRoomFacades.AddOrUpdate(
                                new KeyValuePair<RoomPlan, RoomPlan>(roomPlan, facade.NeighbouringRoom),
                                _ => new List<IConfigurableFacade> { newFacade },
                                (k, v) => { v.Add(newFacade); return v; }
                            );
                        }
                        else
                        {
                            // A facade between these rooms should have already been created, find it and wrap it in a reverse facade
                            List<IConfigurableFacade> fs;
                            if (!interRoomFacades.TryGetValue(new KeyValuePair<RoomPlan, RoomPlan>(facade.NeighbouringRoom, roomPlan), out fs))
                                newFacade = FailedToFindInternalNeighbourSection(facade.NeighbouringRoom, roomPlan, facade);
                            else
                            {
                                // ReSharper disable once AccessToForEachVariableInClosure
                                var f = fs.SingleOrDefault(a => a.Section.Matches(facade.Section));
                                if (f == null)
                                    newFacade = FailedToFindInternalNeighbourSection(facade.NeighbouringRoom, roomPlan, facade);
                                else
                                {
                                    var context = f as ISubdivisionContext;
                                    if (context != null)
                                        context.AddPrerequisite(roomPlan.Node);

                                    newFacade = new ReverseFacade(f, facade.Section);
                                }
                            }
                        }
                    }
                    else
                        newFacade = CreateInternalWall(roomPlan, facade, yOffset);

                    if (newFacade != null)
                        generatedFacades.Add(facade, newFacade);
                }

                roomPlan.Node.Facades = generatedFacades;
                foreach (var context in generatedFacades.Values.OfType<ISubdivisionContext>())
                    context.AddPrerequisite(roomPlan.Node);
            }
        }

        private static IConfigurableFacade FindExternalFacade(float wallThickness, IEnumerable<IConfigurableFacade> facades, LineSegment2D segment)
        {
            return facades.FirstOrDefault(e =>
            {
                var l = e.Section.ExternalLineSegment;

                var edgeDirection = l.Line().Direction;
                var segmentDirection = segment.Line().Direction;

                if (Math.Abs(Vector2.Dot(edgeDirection, segmentDirection)) < 0.99619469809f) //Allow 5 degrees difference
                    return false;

                return
                    Geometry2D.DistanceFromPointToLineSegment(segment.Start, l) < (wallThickness * 5) &&
                    Geometry2D.DistanceFromPointToLineSegment(segment.End, l) < (wallThickness * 5);
            });
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

        protected virtual IConfigurableFacade CreateInternalWall(RoomPlan room, RoomPlan.Facade facade, float yOffset)
        {
            var wall = (IConfigurableFacade)CreateChild(
                new Prism(_roomHeight, facade.Section.A, facade.Section.B, facade.Section.C, facade.Section.D),
                Quaternion.Identity,
                new Vector3(0, yOffset, 0),
                InternalFacadeScripts(room).Where(r => r.Implements<IConfigurableFacade>())
            );

            wall.Section = facade.Section;

            //Make sure the room subdivides before the facade (and thus has a chance to configure it
            ((ISubdivisionContext)wall).AddPrerequisite(room.Node);

            return wall;
        }

        /// <summary>
        /// Internal wall generation failed to find a pregenerated section between two rooms
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="facade"></param>
        /// <returns></returns>
        protected virtual IConfigurableFacade FailedToFindInternalNeighbourSection(RoomPlan a, RoomPlan b, RoomPlan.Facade facade)
        {
            return null;
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
        public abstract IEnumerable<Vector2> PlaceVerticalFeature(VerticalSelection vertical, IReadOnlyList<Vector2> validSpace, IReadOnlyList<IFloor> floors);  

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
