using System;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using Myre.Extensions;

namespace Base_CityGeneration.Elements.Generic
{
    /// <summary>
    /// Fill a parcel in solid with a randomly selected plausible height for a building
    /// </summary>
    [Script("1AA57734-303F-42D8-8986-3881DF17DC95", "Solid Block")]
    public class SolidPlaceholderBuilding
        :ProceduralScript, IGrounded
    {
        public float GroundHeight { get; set; }

        public override bool Accept(Prism bounds, INamedDataProvider parameters)
        {
            return true;
        }

        public override void Subdivide(Prism bounds, ISubdivisionGeometry geometry, INamedDataCollection hierarchicalParameters)
        {
            var height = (float)Math.Min(bounds.Height, Math.Sqrt(bounds.Footprint.Area()) * (Random() + 0.5));
            var material = hierarchicalParameters.GetValue(new TypedName<string>("material")) ?? "concrete";

            var prism = geometry.CreatePrism(material, bounds.Footprint, height).Transform(Matrix.CreateTranslation(0, height / 2f - bounds.Height / 2f + GroundHeight, 0));
            geometry.Union(prism);
        }
    }
}
