using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Generic;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Roads
{
    [Script("0C1517AB-2231-45BF-84E3-85E4780AE852", "Basic Road Junction")]
    public class BasicJunction
        :ProceduralScript, IJunction
    {
        public float GroundHeight { get; set; }

        public Vertex<IVertexBuilder, IHalfEdgeBuilder, IFaceBuilder> Vertex { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            this.CreateFlatPlane(geometry, "tarmac", bounds.Footprint, 1, -1);

            CreateFootpaths(bounds, geometry, hierarchicalParameters, "grass");
        }

        private void CreateFootpaths(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters, string material)
        {
            
        }
    }
}
