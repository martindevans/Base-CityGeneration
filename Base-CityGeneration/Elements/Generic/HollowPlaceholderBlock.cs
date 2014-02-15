using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Generic
{
    /// <summary>
    /// Hollow out an entire region
    /// </summary>
    [Script("13455F94-20E0-40B8-9B6E-00E3ECA37ED1", "Hollow Block")]
    public class HollowPlaceholderBlock
        :ProceduralScript
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            geometry.Subtract(geometry.CreatePrism("concrete", bounds.Footprint, bounds.Height));
        }
    }
}
