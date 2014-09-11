using System;
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
            //  - Find an existing NegotiableFacade between two rooms, and return later (but wrapped in an adapter which reverses access from one side to the other)
            // 2. A facade onto nothing (dead space behind facade)
            //  - Create an IConfigurableFacade, pass it to the room
            // 3. An external wall
            //  - Get relevant external facade and then wrap subsection of it

            // Facades between rooms
            // Key is both rooms (in ID order), value is the facade
            Dictionary<KeyValuePair<RoomPlan, RoomPlan>, List<IConfigurableFacade>> interRoomFacades = new Dictionary<KeyValuePair<RoomPlan, RoomPlan>, List<IConfigurableFacade>>();
            //todo: ^ This is insufficient
            //todo: This room could neighbour another room multiple times which leads to duplicate keys!
            //todo: need to add something extra/change the key, perhaps use section (I previously avoided this due to keys being broken with any deviation of section coordinates)

            foreach (var roomPlan in Plan.Rooms.Where(r => r.Node != null).OrderBy(r => r.Id))
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
                        var externalSection = FindExternalFacade(externalFacades, facade.Section.ExternalLineSegment); 

                        //Create section (or call error handler if no externals ection was found)
                        newFacade = externalSection == null ? FailedToFindExternalSection(roomPlan, facade) : CreateExternalWall(roomPlan, facade, externalSection);
                    }
                    else if (facade.NeighbouringRoom != null && facade.NeighbouringRoom.Node != null)
                    {
                        if (roomPlan.Id < facade.NeighbouringRoom.Id)
                        {
                            //Create a new facade between these rooms and store it for the other room to retrieve later
                            newFacade = CreateInternalWall(roomPlan, facade);
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
                                var f = fs.SingleOrDefault(a => a.Section.Matches(facade.Section, 0.01f));
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
                        newFacade = CreateInternalWall(roomPlan, facade);

                    if (newFacade != null)
                        generatedFacades.Add(facade, newFacade);
                }

                roomPlan.Node.Facades = generatedFacades;
                foreach (var context in generatedFacades.Values.OfType<ISubdivisionContext>())
                    context.AddPrerequisite(roomPlan.Node);
            }
        }

        private static IConfigurableFacade FindExternalFacade(IEnumerable<IConfigurableFacade> facades, LineSegment2D segment)
        {
            return facades.FirstOrDefault(e =>
            {
                var l = e.Section.ExternalLineSegment;

                Geometry2D.Parallelism parallelism;
                Geometry2D.LineLineIntersection(l.Line(), segment.Line(), out parallelism);
                if (parallelism == Geometry2D.Parallelism.Collinear)
                    return true;
                else if (parallelism == Geometry2D.Parallelism.Parallel)
                {
                    return
                        Geometry2D.DistanceFromPointToLineSegment(segment.Start, l) < 0.05f &&
                        Geometry2D.DistanceFromPointToLineSegment(segment.End, l) < 0.05f;
                }
                else
                    return false;
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
