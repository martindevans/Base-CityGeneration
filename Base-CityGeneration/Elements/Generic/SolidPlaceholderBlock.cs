using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Generic
{
    [Script("AEDE0791-79DF-4936-BD3A-56642105D494", "Solid Block")]
    public class SolidPlaceholderBlock
        :ProceduralScript
    {
        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            geometry.Union(geometry.CreatePrism(hierarchicalParameters.GetValue(new TypedName<string>("material")) ?? "concrete", bounds.Footprint, bounds.Height));
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }
    }
}
