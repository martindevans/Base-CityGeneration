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
        :ProceduralScript, IFloor, IFacadeProvider, IRoomPlanProvider
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
            //Get some style parameters
            var externalWallThickness = hierarchicalParameters.GetMaybeValue(new TypedName<float>("external_wall_thickness")) ?? 0.25f;
            var material = hierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Calculate some handy values
            var roomHeight = bounds.Height - _floorThickness - _ceilingThickness;
            var roomOffsetY = -bounds.Height / 2 + roomHeight / 2 + _floorThickness;

            //Create rooms for all the vertical overlaps (and store which room maps to which vertical feature)
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

            //Create plan
            Plan = new FloorPlan(bounds.Footprint);
            CreateRooms(Plan);
            Plan.Freeze();

            // Subtract a solid block from the world (building fills itself in entirely solid, each floor carves itself out)
            ICsgShape brush = geometry.CreatePrism(material, Plan.ExternalFootprint.ToArray(), roomHeight).Translate(new Vector3(0, roomOffsetY, 0));
            foreach (var roomPlan in Plan.Rooms)
            {
                // Extend floor brush to cut holes in ceiling and floor for vertical sections
                if (verticalSubsections.ContainsKey(roomPlan))
                    brush = brush.Union(geometry.CreatePrism(material, roomPlan.OuterFootprint, bounds.Height));
            }
            geometry.Subtract(brush);

            //Create room scripts
            foreach (var roomPlan in Plan.Rooms)
            {
                var room = (IRoom)CreateChild(new Prism(roomHeight, roomPlan.InnerFootprint), Quaternion.Identity, new Vector3(0, roomOffsetY, 0), roomPlan.Scripts.Where(r => r.Implements<IRoom>()));
                if (room != null)
                    roomPlan.Node = room;
            }

            //Create external facades
            var externalSections = new List<IExternalFacade>();
            var outside = Plan.ExternalFootprint;
            foreach (var section in outside.Sections(externalWallThickness))
            {
                var s = (IExternalFacade)CreateChild(new Prism(bounds.Height, section.A, section.B, section.C, section.D), Quaternion.Identity, Vector3.Zero,
                    ExternalFacadeScripts().Where(e => e.Implements<IExternalFacade>())
                );
                if (s != null)
                {
                    s.Section = section;
                    externalSections.Add(s);
                }
            }

            //Create facades for rooms
            //There are three types of facade:
            // 1. A border between 2 rooms
            //  - Create a NegotiableFacade between rooms and store for later retrieval
            // 2. A facade onto nothing (dead space behind facade)
            //  - Do nothing - room can create this facade itself
            // 3. An external wall
            //  - Get relevant external facade and then wrap subsection of it
            foreach (var roomPlan in Plan.Rooms)
            {
                RoomPlan plan = roomPlan;
                var neighbours = Plan.GetNeighbours(roomPlan).Where(a => a.Other(plan).Id < plan.Id).ToArray();
            }
        }

        /// <summary>
        /// Call Plan.AddRoom to put new rooms into the floor plan
        /// </summary>
        /// <param name="plan"></param>
        protected abstract void CreateRooms(FloorPlan plan);

        protected abstract IEnumerable<ScriptReference> ExternalFacadeScripts();

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

        #region IRoomPlanProvider implementation
        public RoomPlan GetPlan(IPlannedRoom room)
        {
            return Plan.Rooms.SingleOrDefault(r => ReferenceEquals(r.Node, room));
        }
        #endregion

        #region helpers

        #endregion
    }
}
