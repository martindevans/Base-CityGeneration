using Base_CityGeneration.Elements.Building.Internals.VerticalFeatures;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.TestHelpers.Scripts
{
    [Script("E6269FC4-FCEF-4E91-9472-079CB56171D5", "Blank Test Vertical")]
    public class BlankTestVertical
        :ProceduralScript, IVerticalFeature
    {
        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
        }

        public int BottomFloorIndex { get; set; }

        public int TopFloorIndex { get; set; }
    }
}
