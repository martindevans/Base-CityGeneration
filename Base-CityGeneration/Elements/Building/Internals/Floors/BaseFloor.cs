using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using Myre.Extensions;
using SwizzleMyVectors;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using SwizzleMyVectors.Geometry;

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

        public float FloorAltitude { get; set; }

        public float FloorHeight { get; set; }

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

        #region floors and ceilings
        private void CreateFloors(Prism bounds, ISubdivisionGeometry geometry, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections, string material)
        {
            Contract.Requires(geometry != null, "geometry != null");
            Contract.Requires(verticalSubsections != null, "verticalSubsections != null");

            var floor = geometry.CreatePrism(material, bounds.Footprint, _floorThickness).Translate(new Vector3(0, -bounds.Height / 2 + _floorThickness / 2, 0));

            floor = CutVerticalHoles(floor, geometry, material, verticalSubsections);

            geometry.Union(floor);
        }

        private void CreateCeilings(Prism bounds, ISubdivisionGeometry geometry, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections, string material)
        {
            Contract.Requires(geometry != null, "geometry != null");
            Contract.Requires(verticalSubsections != null, "verticalSubsections != null");

            var ceiling = geometry.CreatePrism(material, bounds.Footprint, _ceilingThickness).Translate(new Vector3(0, bounds.Height / 2 - _ceilingThickness / 2, 0));

            ceiling = CutVerticalHoles(ceiling, geometry, material, verticalSubsections);

            geometry.Union(ceiling);
        }

        private ICsgShape CutVerticalHoles(ICsgShape shape, ISubdivisionGeometry geometry, string material, Dictionary<RoomPlan, IVerticalFeature> verticalSubsections)
        {
            Contract.Requires(shape != null, "shape != null");
            Contract.Requires(geometry != null, "geometry != null");
            Contract.Requires(verticalSubsections != null, "verticalSubsections != null");
            Contract.Ensures(Contract.Result<ICsgShape>() != null);

            var shapeHeight = (shape.Bounds.Max.Y - shape.Bounds.Min.Y) * 2f;
            var shapeMid = (shape.Bounds.Min.Y + shape.Bounds.Max.Y) * 0.5f;

            foreach (var verticalSubsection in verticalSubsections)
            {
                shape = shape.Subtract(
                    geometry.CreatePrism(material, verticalSubsection.Key.OuterFootprint, shapeHeight).Translate(new Vector3(0, shapeMid, 0))
                );
                Contract.Assume(shape != null);
            }

            return shape;
        }
        #endregion

        #region facades
        private List<IConfigurableFacade> CreateExternalFacades(Prism bounds)
        {
            var externalSections = new List<IConfigurableFacade>();

            //Find the parent building which contains this floor
            var building = TreeSearch.SearchUp<IBuilding, IBuilding>(this, n => n, typeof(IBuildingContainer));
            if (building == null)
                throw new InvalidOperationException("Attempted to subdivide BaseFloor, but cannot find IBuilding node ancestor");

            //Get all facades which cross this floor
            var facades = building.Facades(FloorIndex);

            for (int i = 0; i < Plan.ExternalFootprint.Count; i++)
            {
                //Nb. There's lots of "WS" going on here, this stands for "World Space"
                //We have the footprint in floor space and the facades in facade space, we transform both into world space to compare them

                //Get start and end points of this edge
                var start = Plan.ExternalFootprint[i];
                var end = Plan.ExternalFootprint[(i + 1) % Plan.ExternalFootprint.Count];
                var footprintSegWS = new LineSegment2(start, end).Transform(WorldTransformation);
                var footprintLineWS = footprintSegWS.Line;

                //Select the exteral facade which lies along this edge
                var wall = (from facade in facades
                            let facadeSegWS = facade.Section.ExternalLineSegment.Transform(facade.WorldTransformation)
                            let facadeLineWS = facadeSegWS.Line
                            where facadeLineWS.Parallelism(footprintLineWS) != Parallelism.None
                            let aD = footprintSegWS.DistanceToPoint(facadeSegWS.Start)
                            let bD = footprintSegWS.DistanceToPoint(facadeSegWS.End)
                            orderby aD + bD
                            select facade).FirstOrDefault();

                //This happens in cases where the building didn't generate an external facade for a section (e.g. section too small to fit a facade in)
                //If the building didn't generate a facade, obviously the floor can't find it!
                if (wall == null)
                    continue;

                //Start and end points (X-Axis) are always start and end of facade (i.e. subsection is always full width)
                //What are the start and end points (Y-Axis)
                var bottomOfFacade = building.Floor(wall.BottomFloorIndex).FloorAltitude;
                var y = FloorAltitude - bottomOfFacade - _floorThickness - wall.Bounds.Height / 2;

                //Height of the open space of the floor (top of floor, to bottom of ceiling)
                var height = FloorHeight - _floorThickness - _ceilingThickness;

                //how wide is the wall?
                var wallLength = wall.Section.ExternalLineSegment.LongLine.Direction.Length();

                SubsectionFacade subsection = new SubsectionFacade(wall,
                    new Vector2(-wallLength, y),
                    new Vector2(wallLength, y + height),
                    0, 1,
                    wall.Section
                );

                externalSections.Add(subsection);
            }

            return externalSections;
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

        private static IConfigurableFacade FindExternalFacade(float wallThickness, IEnumerable<IConfigurableFacade> externalFacades, LineSegment2 segment)
        {
            return externalFacades.FirstOrDefault(e =>
            {
                var l = e.Section.ExternalLineSegment;

                var edgeDirection = l.Line.Direction;
                var segmentDirection = segment.Line.Direction;

                if (Math.Abs(Vector2.Dot(edgeDirection, segmentDirection)) < 0.99619469809f) //Allow 5 degrees difference
                    return false;

                return
                    l.DistanceToPoint(segment.Start) < (wallThickness * 5) &&
                    l.DistanceToPoint(segment.End) < (wallThickness * 5);
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
            ((ISubdivisionContext)externalSection).AddPrerequisite(roomPlan.Node);

            //Calculate X position of subsection (map room section onto full wall section)
            var at = externalSection.Section.InternalLineSegment.LongLine.ClosestPointDistanceAlongLine(facade.Section.ExternalLineSegment.Start);
            var bt = externalSection.Section.InternalLineSegment.LongLine.ClosestPointDistanceAlongLine(facade.Section.ExternalLineSegment.End);

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
            Contract.Requires(room != null);
            Contract.Requires(facade != null);
            Contract.Ensures(Contract.Result<IConfigurableFacade>() != null);

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
        /// Return a set of possible scripts to use for internal facades. Must implement IConfigurableFacade
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> InternalFacadeScripts(RoomPlan roomPlan)
        {
            yield return new ScriptReference(typeof(ConfigurableFacade));
        }

        /// <summary>
        /// Return a set of possible scripts to use for internal (neighbour) facades. Must implement IConfigurableFacade
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <returns></returns>
        protected virtual IEnumerable<ScriptReference> InternalNeighbourFacadeScripts(RoomPlan roomPlan)
        {
            yield return new ScriptReference(typeof(ConfigurableFacade));
        }
        #endregion

        #region rooms
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

                //Create a room using the identity script
                //  Consider:
                //    - Should we use something other than the empty room for verticals?
                //    - Perhaps allow vertical elements to supply their own room script?
                var r = plan.AddRoom(points, 0.1f, new[] { new ScriptReference(typeof(EmptyRoom)) }).Single();
                return new KeyValuePair<RoomPlan, IVerticalFeature>(r, overlap);
            }).ToDictionary(a => a.Key, a => a.Value);
            return verticalSubsections;
        }

        private void CreateFloorPlan(Prism bounds, FloorPlan plan)
        {
            CreateRooms(plan);
            plan.Freeze();
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
