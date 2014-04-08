using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads
{
    [Script("0C1517AB-2231-45BF-84E3-85E4780AE852", "Basic Road Junction")]
    public class BasicJunction
        :BigFlatPlane, IJunction
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

        public void AttachRoad(IRoad road)
        {
        }
    }
}
