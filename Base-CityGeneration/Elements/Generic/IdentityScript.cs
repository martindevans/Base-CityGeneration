using EpimetheusPlugins.Procedural;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Generic
{
    public class IdentityScript
        :ProceduralScript
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
