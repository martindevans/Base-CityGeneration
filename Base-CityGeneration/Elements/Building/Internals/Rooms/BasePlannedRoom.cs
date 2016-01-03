using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Styles;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using System.Numerics;
using Myre;
using Myre.Collections;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    [Script("E655C852-8B0E-460B-BD30-35158DA1053C", "Base Room")]
    public class BasePlannedRoom
        : ProceduralScript, IPlannedRoom, IDoorPlacer, IDoorTarget
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

        public IReadOnlyDictionary<RoomPlan.Facade, IConfigurableFacade> Facades { protected get; set; }

        private readonly ISet<IPlannedRoom> _connectTo = new HashSet<IPlannedRoom>();
        protected IEnumerable<IPlannedRoom> ConnectDoorsTo
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IPlannedRoom>>() != null);
                return _connectTo;
            }
        }

        protected void PlaceConnections(Prism bounds, IEnumerable<IPlannedRoom> targets)
        {
            var doorWidth = HierarchicalParameters.StandardDoorWidth(Random);
            var material = HierarchicalParameters.GetValue(new TypedName<string>("material"));

            //Doors in walls to neighbours where requested
            foreach (var facade in Facades.Where(f => f.Key.NeighbouringRoom != null && targets.Contains(f.Key.NeighbouringRoom.Node)))
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

        public virtual bool ConnectTo(IPlannedRoom otherRoom)
        {
            if (otherRoom != null)
            {
                _connectTo.Add(otherRoom);
                return true;
            }
            return false;
        }

        public virtual bool AllowConnectionTo(IPlannedRoom other)
        {
            return true;
        }
    }
}
