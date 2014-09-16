using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    [Script("E655C852-8B0E-460B-BD30-35158DA1053C", "Base Room")]
    public class BasePlannedRoom
        : ProceduralScript, IPlannedRoom, IDoorPlacer
    {
        private readonly bool _placeRequestedDoorConnections;

        public BasePlannedRoom(bool placeRequestedDoorConnections = true)
        {
            _placeRequestedDoorConnections = placeRequestedDoorConnections;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            if (_placeRequestedDoorConnections)
                PlaceConnections(bounds, ConnectDoorsTo);
        }

        public Dictionary<RoomPlan.Facade, IConfigurableFacade> Facades { protected get; set; }

        private readonly ISet<RoomPlan> _connectTo = new HashSet<RoomPlan>();
        protected IEnumerable<RoomPlan> ConnectDoorsTo
        {
            get { return _connectTo; }
        }

        protected void PlaceConnections(Prism bounds, IEnumerable<RoomPlan> targets)
        {
            var doorWidth = HierarchicalParameters.StandardDoorWidth(Random);
            var material = HierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Doors in walls to neighbours where requested
            foreach (var facade in Facades.Where(f => f.Key.NeighbouringRoom != null && targets.Contains(f.Key.NeighbouringRoom)))
            {
                var dw = MathHelper.Min(doorWidth, facade.Key.Section.Width * 0.9f);

                facade.Value.AddStamp(new BaseFacade.Stamp(0, 1, false, material,
                    new Vector2(-dw / 2, -bounds.Height / 2),
                    new Vector2(-dw / 2, bounds.Height / 2),
                    new Vector2(dw / 2, bounds.Height / 2),
                    new Vector2(dw / 2, -bounds.Height / 2)
                ));
            }
        }

        public bool ConnectTo(RoomPlan otherRoom)
        {
            if (otherRoom != null)
            {
                _connectTo.Add(otherRoom);
                return true;
            }
            return false;
        }
    }
}
