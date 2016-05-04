using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using Myre.Collections;
using SwizzleMyVectors;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Extensions;
using Base_CityGeneration.Styles;
using ClipperLib;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Scripts;
using HandyCollections.Extensions;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors
{
    /// <summary>
    /// A floor placed into a section of empty space.
    /// </summary>
    public abstract class BaseFloor
        : ProceduralScript, IFloor
    {
        #region fields and properties
        private readonly float _minHeight;
        private readonly float _maxHeight;
        private readonly float _floorThickness;
        private readonly float _ceilingThickness;

        private float _roomHeight;

        private IFloorPlan _plan;

        private int? _floorIndex;
        /// <summary>
        /// The index of this floor (must be set before subdivision)
        /// </summary>
        public int FloorIndex
        {
            get
            {
                if (!_floorIndex.HasValue)
                    throw new InvalidOperationException("Cannot get floor index before it has been set");
                return _floorIndex.Value;
            }
            set
            {
                if (State != SubdivisionStates.NotSubdivided)
                    throw new InvalidOperationException("Cannot set floor index after subdivision");
                _floorIndex = value;
            }
        }

        private float? _floorAltitude;
        /// <summary>
        /// The altitude of this floor (must be set before subdivision)
        /// </summary>
        public float FloorAltitude
        {
            get
            {
                if (!_floorAltitude.HasValue)
                    throw new InvalidOperationException("Cannot get floor altitude before it has been set");
                return _floorAltitude.Value;
            }
            set
            {
                if (State != SubdivisionStates.NotSubdivided)
                    throw new InvalidOperationException("Cannot set floor altitude after subdivision");
                _floorAltitude = value;
            }
        }
        #endregion

        #region construction
        protected BaseFloor(float minHeight = 1.4f, float maxHeight = 2.5f, float floorThickness = 0.1f, float ceilingThickness = 0.1f)
        {
            _minHeight = minHeight;
            _maxHeight = maxHeight;
            _floorThickness = floorThickness;
            _ceilingThickness = ceilingThickness;
        }
        #endregion

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            var min = _minHeight + _floorThickness + _ceilingThickness;
            var max = _maxHeight + _floorThickness + _ceilingThickness;

            return bounds.Height >= min && bounds.Height <= max;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            //Sanity checks
            if (!_floorIndex.HasValue)
                throw new InvalidOperationException("Attempted to subdivide BaseFloor, but FloorIndex is not set");
            if (!_floorAltitude.HasValue)
                throw new InvalidOperationException("Attempted to subdivide BaseFloor, but FloorAltitude is not set");

            //Calculate some handy values
            _roomHeight = bounds.Height - _floorThickness - _ceilingThickness;
            var roomOffsetY = -bounds.Height / 2 + _roomHeight / 2 + _floorThickness;

            //Find vertical elements which start on this floor
            var constrainedVerticalElements = ConstrainVerticalElements(this.SearchUp<IBuilding, IBuilding>(a => a, typeof(IBuildingContainer)));

            //Create a plan for this floor
            var plan = new Plan.Geometric.GeometricFloorplan(Bounds.Footprint);
            var overlappingVerticalRooms = InsertOverlappingVerticals(
                plan,
                this.SearchUp<IVerticalFeatureContainer, IVerticalFeatureContainer>(a => a, typeof(IBuildingContainer)).Overlapping(FloorIndex, false)
            );

            var verticals = CreateFloorPlan(plan, overlappingVerticalRooms, constrainedVerticalElements).ToArray();
            _plan = plan.Freeze();

            PlanFrozen(_plan);

            //Create nodes for all the vertical elements which started on this floor
            CreateVerticalNodes(verticals);

            //Create Floor and ceiling (with holes for vertical sections)
            CreateFloors(bounds, geometry, verticals, Metadata.DefaultCeilingMaterial(Random));
            CreateCeilings(bounds, geometry, verticals, Metadata.DefaultCeilingMaterial(Random));

            //Create room scripts
            CreateRoomScripts(roomOffsetY, _roomHeight, _plan);

            //Create external facades (subsections of building over this floor facade)
            var externalFacades = CreateExternalFacades(bounds, _plan);

            //Create facades for rooms
            var dist = hierarchicalParameters.ExternalWallThickness(Random);
            CreateRoomFacades(externalFacades, roomOffsetY, dist, _plan);
        }

        #region verticals
        /// <summary>
        /// Get a set of vertical elements which start on this floor
        /// </summary>
        /// <param name="parentBuilding"></param>
        /// <returns></returns>
        private IReadOnlyList<ConstrainedVerticalSelection> ConstrainVerticalElements(IBuilding parentBuilding)
        {
            //Get all vertical selections which start on this floor
            var starting = parentBuilding
                .Verticals(FloorIndex, FloorIndex)
                .Where(a => a.StartingFloor() == FloorIndex)
                .ToArray();

            //Create somewhere to put results
            var results = new ConstrainedVerticalSelection[starting.Length];

            //Inspect all the floors which overlap each vertical, and constrain the area the vertical can be placed in
            for (var i = 0; i < starting.Length; i++)
            {
                var verticalSelection = starting[i];

                //Get all floors this feature overlaps
                var crossedFloors = (
                    from f in Enumerable.Range(verticalSelection.Bottom, verticalSelection.Top - verticalSelection.Bottom + 1)
                    select parentBuilding.Floor(f)
                ).ToArray();

                //Calculate the intersection of all crossed floor footprints
                var intersection = IntersectionOfFootprints(crossedFloors);

                results[i] = new ConstrainedVerticalSelection(verticalSelection, intersection);
            }

            return results;
        }

        /// <summary>
        /// Calculate intersection of all footprints in the given set of floors (converted into the space of this floor)
        /// </summary>
        /// <param name="floors"></param>
        /// <returns></returns>
        private IReadOnlyList<Vector2> IntersectionOfFootprints(IReadOnlyList<IFloor> floors)
        {
            Contract.Requires(floors != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);

            var c = new Clipper();

            //Transform all floor footprints into the space of this floor, and intersect them
            //This is the allowable space for placing a new vertical element between these floors
            var results = c.IntersectAll(floors
                .Select(f => f
                    .Bounds
                    .Footprint
                    .Select(a => Vector3
                        .Transform(a.X_Y(0), f.InverseWorldTransformation * WorldTransformation)
                        .XZ()
                    )
                )
            ).ToArray();

            //Return the one with the largest area
            return results
                .Select(a => a.ToArray())
                .MaxItem(a => Math.Abs(a.Area()));
        }

        /// <summary>
        /// Materialise vertical elements (with room plans) into actual procedural nodes
        /// </summary>
        /// <param name="verticals"></param>
        private void CreateVerticalNodes(IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> verticals)
        {
            var container = this.SearchUp<IVerticalFeatureContainer, IVerticalFeatureContainer>(a => a, typeof(IBuildingContainer));
            var building = this.SearchUp<IBuilding, IBuilding>(a => a, typeof(IBuildingContainer));

            foreach (var keyValuePair in verticals)
            {
                //vertical features are created by the bottom floor which they overlap, so ignore all other verticals
                if (keyValuePair.Key.Bottom != FloorIndex)
                    continue;
                
                //Get all floors which this vertical elements crossed
                var crossedFloors = (
                    from i in Enumerable.Range(keyValuePair.Key.Bottom, keyValuePair.Key.Top - keyValuePair.Key.Bottom + 1)
                    select building.Floor(i)
                ).ToArray();

                //Transform from floor space into building space
                var transform = InverseWorldTransformation * building.WorldTransformation;
                var bFootprint = keyValuePair.Value.OuterFootprint.Select(a => Vector3.Transform(a.X_Y(0), transform).XZ()).Clockwise().ToArray();

                //Calculate height of the vertical element
                var height = crossedFloors.Sum(a => a.Bounds.Height);

                //Create vertical element node
                var node = (IVerticalFeature)CreateChild(
                    new Prism(height, bFootprint),
                    Quaternion.Identity,
                    new Vector3(0, height / 2, 0),
                    false,                          //Do *not* check that the vertical element is contained within parent
                                                    //We don't expect a vertical to be contained within a single floor!
                    keyValuePair.Key.Script
                );
                node.BottomFloorIndex = keyValuePair.Key.Bottom;
                node.TopFloorIndex = keyValuePair.Key.Top;

                //Pass this vertical back up to the parent container
                container.Add(keyValuePair.Key, node);

                //Ensure all floors this vertical crosses subdivide before the vertical
                foreach (var crossedFloor in crossedFloors)
                    node.AddPrerequisite(crossedFloor);
            }
        }
        #endregion

        #region floors and ceilings
        private void CreateFloors(Prism bounds, ISubdivisionGeometry geometry, IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> verticalSubsections, string material)
        {
            Contract.Requires(geometry != null);
            Contract.Requires(verticalSubsections != null);

            var floor = geometry.CreatePrism(material, bounds.Footprint, _floorThickness).Translate(new Vector3(0, -bounds.Height / 2 + _floorThickness / 2, 0));

            floor = CutVerticalHoles(floor, geometry, material, verticalSubsections.Select(a => a.Value));

            geometry.Union(floor);
        }

        private void CreateCeilings(Prism bounds, ISubdivisionGeometry geometry, IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> verticalSubsections, string material)
        {
            Contract.Requires(geometry != null);
            Contract.Requires(verticalSubsections != null);

            var ceiling = geometry.CreatePrism(material, bounds.Footprint, _ceilingThickness).Translate(new Vector3(0, bounds.Height / 2 - _ceilingThickness / 2, 0));

            ceiling = CutVerticalHoles(ceiling, geometry, material, verticalSubsections.Select(a => a.Value));

            geometry.Union(ceiling);
        }

        private static ICsgShape CutVerticalHoles(ICsgShape shape, ISubdivisionGeometry geometry, string material, IEnumerable<IRoomPlan> verticalSubsections)
        {
            Contract.Requires(shape != null);
            Contract.Requires(geometry != null);
            Contract.Requires(verticalSubsections != null);
            Contract.Ensures(Contract.Result<ICsgShape>() != null);

            var shapeHeight = (shape.Bounds.Max.Y - shape.Bounds.Min.Y) * 2f;
            var shapeMid = (shape.Bounds.Min.Y + shape.Bounds.Max.Y) * 0.5f;

            foreach (var verticalSubsection in verticalSubsections)
            {
                shape = shape.Subtract(
                    geometry.CreatePrism(material, verticalSubsection.OuterFootprint, shapeHeight).Translate(new Vector3(0, shapeMid, 0))
                );
                Contract.Assume(shape != null);
            }

            return shape;
        }
        #endregion

        #region facades
        private IReadOnlyList<IConfigurableFacade> CreateExternalFacades(Prism bounds, IFloorPlan plan)
        {
            var externalSections = new List<IConfigurableFacade>();

            //Find the parent building which contains this floor
            var building = this.SearchUp<IBuilding, IBuilding>(n => n, typeof(IBuildingContainer));
            if (building == null)
                throw new InvalidOperationException("Attempted to subdivide BaseFloor, but cannot find IBuilding node ancestor");

            //Get all facades which cross this floor
            var facades = building.Facades(FloorIndex);

            for (var i = 0; i < plan.ExternalFootprint.Count; i++)
            {
                //Nb. There's lots of "WS" going on here, this stands for "World Space"
                //We have the footprint in floor space and the facades in facade space, we transform both into world space to compare them

                //Get start and end points of this edge
                var start = plan.ExternalFootprint[i];
                var end = plan.ExternalFootprint[(i + 1) % plan.ExternalFootprint.Count];
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

                //If we didn't find a parallel external facade then just give up!
                if (wall == null)
                    continue;

                //Start and end points (X-Axis) are always start and end of facade (i.e. subsection is always full width)
                //What are the start and end points (Y-Axis)
                var bottomOfFacade = building.Floor(wall.BottomFloorIndex).FloorAltitude;
                var y = FloorAltitude - bottomOfFacade - _floorThickness - wall.Bounds.Height / 2;

                //Height of the open space of the floor (top of floor, to bottom of ceiling)
                var height = Bounds.Height - _floorThickness - _ceilingThickness;

                //how wide is the wall?
                var wallLength = wall.Section.ExternalLineSegment.LongLine.Direction.Length();

                var subsection = new SubsectionFacade(wall,
                    new Vector2(-wallLength, y),
                    new Vector2(wallLength, y + height),
                    0, 1,
                    wall.Section
                );

                externalSections.Add(subsection);
            }

            return externalSections;
        }

        private void CreateRoomFacades(IReadOnlyCollection<IConfigurableFacade> externalFacades, float yOffset, float distance, IFloorPlan plan)
        {
            Contract.Requires(externalFacades != null);
            Contract.Requires(plan != null);

            //There are two types of facade:
            // 1. An external wall
            //  - Find the relevant external facade and wrap a subsection of it
            // 2. An internal wall
            //  - Create an IConfigurableFacade

            foreach (var roomPlan in plan.Rooms.Where(r => r.Node != null).OrderBy(r => r.Id))
            {
                //Create a place to store all facades generated for this room
                var generatedFacades = new Dictionary<Facade, IConfigurableFacade>();

                foreach (var facade in roomPlan.GetWalls())
                {
                    IConfigurableFacade newFacade;

                    if (facade.IsExternal)
                    {
                        //Find the external wall which is co-linear with this facade section
                        var externalSection = FindExternalFacade(distance * 3, externalFacades, facade.Section.ExternalLineSegment);

                        //Create section (or call error handler if no external section was found)
                        newFacade = externalSection == null ? FailedToFindExternalSection(roomPlan, facade) : CreateExternalWall(roomPlan, facade, externalSection);
                    }
                    else
                    {
                        newFacade = CreateInternalWall(roomPlan, facade, yOffset);
                    }

                    //Store the newly created facade
                    if (newFacade != null)
                        generatedFacades.Add(facade, newFacade);
                }

                //Pass the facade to the room
                if (roomPlan.Node != null)
                {
                    //Pass the facades to the room
                    roomPlan.Node.Facades = generatedFacades;

                    //Ensure that the room subdivides before the facades
                    foreach (var context in generatedFacades.Values.OfType<ISubdivisionContext>())
                        context.AddPrerequisite(roomPlan.Node);
                }
            }
        }

        private static IConfigurableFacade FindExternalFacade(float distance, IEnumerable<IConfigurableFacade> externalFacades, LineSegment2 segment)
        {
            Contract.Requires(externalFacades != null);

            return externalFacades.FirstOrDefault(e =>
            {
                var l = e.Section.ExternalLineSegment;

                var edgeDirection = l.Line.Direction;
                var segmentDirection = segment.Line.Direction;

                if (Math.Abs(Vector2.Dot(edgeDirection, segmentDirection)) < 0.999847695f) //Allow 1 degrees difference
                    return false;

                return
                    l.DistanceToPoint(segment.Start) < distance &&
                    l.DistanceToPoint(segment.End) < distance;
            });
        }

        /// <summary>
        /// External wall generation failed to find a setion which is co-linear with the given facade section.
        /// Default behaviour is to return null to do nothing
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <param name="facade"></param>
        // ReSharper disable UnusedParameter.Global (Justification: External API)
        protected virtual IConfigurableFacade FailedToFindExternalSection(IRoomPlan roomPlan, Facade facade)
        // ReSharper restore UnusedParameter.Global
        {
            return null;
        }

        /// <summary>
        /// Create a wall section as a subsection along the given external facade section
        /// </summary>
        /// <param name="roomPlan"></param>
        /// <param name="facade"></param>
        /// <param name="externalSection"></param>
        /// <returns></returns>
        // ReSharper disable once VirtualMemberNeverOverriden.Global (Justification: External API)
        protected virtual IConfigurableFacade CreateExternalWall(IRoomPlan roomPlan, Facade facade, IConfigurableFacade externalSection)
        {
            Contract.Requires(roomPlan != null);
            Contract.Requires(facade != null);
            Contract.Requires(externalSection != null);

            //Make sure the room subdivides before the facade (and thus has a chance to configure it
            externalSection.GetDependencyContext().AddPrerequisite(roomPlan.Node);

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

        // ReSharper disable once VirtualMemberNeverOverriden.Global (Justification: External API)
        protected virtual IConfigurableFacade CreateInternalWall(IRoomPlan room, Facade facade, float yOffset)
        {
            Contract.Requires(room != null);
            Contract.Requires(facade != null);
            Contract.Ensures(Contract.Result<IConfigurableFacade>() != null);

            var wall = (IConfigurableFacade)CreateChild(
                new Prism(_roomHeight, facade.Section.GetCorners()),
                Quaternion.Identity,
                new Vector3(0, yOffset, 0),
                InternalFacadeScripts(room)
            );

            wall.Section = facade.Section;

            //Make sure the room subdivides before the facade (and thus has a chance to configure it
            ((ISubdivisionContext)wall).AddPrerequisite(room.Node);

            return wall;
        }

        // ReSharper disable once VirtualMemberNeverOverriden.Global (Justification: External API)
        // ReSharper disable once UnusedParameter.Global (Justification: External API)
        protected virtual IEnumerable<KeyValuePair<float, ScriptReference>> InternalFacadeScripts(IRoomPlan room)
        {
            yield return new KeyValuePair<float, ScriptReference>(1, new ScriptReference(typeof(ConfigurableFacade)));
        }
        #endregion

        #region rooms
        private IReadOnlyDictionary<IRoomPlan, KeyValuePair<VerticalSelection, IVerticalFeature>> InsertOverlappingVerticals(IFloorPlanBuilder plan, IEnumerable<KeyValuePair<VerticalSelection, IVerticalFeature>> overlappingElements)
        {
            var result = new Dictionary<IRoomPlan, KeyValuePair<VerticalSelection, IVerticalFeature>>();

            foreach (var element in overlappingElements)
            {
                //Transform overlap into coordinate frame of floor
                var points = element.Value.Bounds.Footprint.ToArray();
                var w = element.Value.WorldTransformation * InverseWorldTransformation;
                for (var i = 0; i < points.Length; i++)
                    points[i] = Vector3.Transform(points[i].X_Y(0), w).XZ();

                //Ensure Clockwise winding
                if (points.Area() < 0)
                    Array.Reverse(points);

                //Create a room which is the space of this vertical element
                var r = plan.Add(points, Metadata.InternalWallThickness(Random)).Single();

                //set room to use identity script
                //  Consider:
                //    - Should we use something other than the empty room for verticals?
                //    - Perhaps allow vertical elements to supply their own room script?
                r.AddScript(1, new ScriptReference(typeof(IdentityRoom)));

                //Save the result
                result.Add(r, element);
            }

            return result;
        }

        private void CreateRoomScripts(float yOffset, float height, IFloorPlan plan)
        {
            Contract.Requires(plan != null);

            foreach (var roomPlan in plan.Rooms)
            {
                //Create the room (engine chooses which script to take from the options presented)
                var room = CreateChild(
                    new Prism(height, roomPlan.InnerFootprint),
                    Quaternion.Identity,
                    new Vector3(0, yOffset, 0),
                    roomPlan.Scripts
                );

                var planned = room as IPlannedRoom;
                if (planned != null)
                {
                    //Associate plan with room
                    planned.Plan = roomPlan;

                    //Associate room with plan
                    roomPlan.Node = planned;
                }
            }
        }
        #endregion

        #region abstracts
        /// <summary>
        /// Called once the plan has been frozen
        /// </summary>
        /// <param name="plan"></param>
        // ReSharper disable once UnusedParameter.Global (Justification: External API)
        // ReSharper disable once VirtualMemberNeverOverriden.Global (Justification: External API)
        protected virtual void PlanFrozen(IFloorPlan plan)
        {
        }

        /// <summary>
        /// Insert rooms into the given floorplan
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="overlappingVerticalElements">Map from rooms to verticals which they were created for</param>
        /// <param name="constrainedVerticalElements"></param>
        /// <returns></returns>
        protected abstract IEnumerable<KeyValuePair<VerticalSelection, IRoomPlan>> CreateFloorPlan(IFloorPlanBuilder builder, IReadOnlyDictionary<IRoomPlan, KeyValuePair<VerticalSelection, IVerticalFeature>> overlappingVerticalElements, IReadOnlyList<ConstrainedVerticalSelection> constrainedVerticalElements);
        #endregion
    }
}
