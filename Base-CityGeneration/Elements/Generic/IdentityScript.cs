using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Generic
{
    [Script("5B7CCEC6-D1A9-4EBA-8DA4-AEC6222AF565", "Identity Script (Empty)")]
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
