//using System.Collections.Generic;
//using System.Diagnostics.Contracts;
//using System.Linq;
//using Base_CityGeneration.Elements.Building.Facades;
//using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
//using Base_CityGeneration.Styles;
//using EpimetheusPlugins.Procedural;
//using EpimetheusPlugins.Scripts;
//using System.Numerics;
using System.Collections.Generic;
using Base_CityGeneration.Elements.Building.Facades;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using EpimetheusPlugins.Procedural;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Rooms
{
    public abstract class BasePlannedRoom
        : ProceduralScript, IPlannedRoom
    {
        public IRoomPlan Plan { get; set; }

        public IReadOnlyDictionary<Facade, IConfigurableFacade> Facades { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
        }
    }
}
