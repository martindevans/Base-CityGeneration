using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    [Script("E655C852-8B0E-460B-BD30-35158DA1053C", "Base Room")]
    public class BasePlannedRoom
        : ProceduralScript, IPlannedRoom
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
        }

        public Dictionary<RoomPlan.Facade, IConfigurableFacade> Facades { protected get; set; }
    }
}
