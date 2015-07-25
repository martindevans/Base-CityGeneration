using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Generic
{
    /// <summary>
    /// Create a flat plane at the bottom of a region
    /// </summary>
    [Script("F072AFE1-33C6-4BA5-B17D-64FFA2F25CDE", "Big Flat Plane")]
    public class BigFlatPlane
        : ProceduralScript, IGrounded
    {
        public float GroundHeight
        {
            get;
            set;
        }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            var height = hierarchicalParameters.GetMaybeValue(new TypedName<float>("height")) ?? 1;

            this.CreateFlatPlane(geometry,
                hierarchicalParameters.GetValue(new TypedName<string>("material")) ?? "grass",
                bounds.Footprint,
                height,
                -height
            );
        }
    }
}
