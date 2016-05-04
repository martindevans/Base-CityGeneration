using Base_CityGeneration.Elements.Building.Facades;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.TestHelpers.Scripts
{
    [Script("1FF4EC22-49E3-4E84-877A-DCAF27C8643E", "Blank Test Facade")]
    public class DefaultTestFacade
        : BaseBuildingFacade
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
        }
    }
}
