using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Types.Roads;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads
{
    [Script("0C714177-06D9-4E20-8028-FA0CE6519892", "Basic Road")]
    public class BasicRoad
        :BigFlatPlane, IRoad
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            hierarchicalParameters.Set(new TypedName<string>("material"), "tarmac");
            hierarchicalParameters.Set(new TypedName<float>("height"), 1);

            base.Subdivide(bounds, geometry, hierarchicalParameters);
        }

        public HalfEdge Edge
        {
            get;
            set;
        }

        public float Width
        {
            get;
            set;
        }
    }
}
