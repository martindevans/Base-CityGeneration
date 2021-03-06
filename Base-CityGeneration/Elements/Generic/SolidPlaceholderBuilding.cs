﻿using Base_CityGeneration.Styles;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Scripts;
using System.Numerics;
using Myre.Collections;
using System;

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
            var height = (float)Math.Min(bounds.Height, Math.Sqrt(Math.Abs(bounds.Footprint.Area())) * (Random() + 0.5));
            var material = hierarchicalParameters.DefaultMaterial(Random);

            var prism = geometry.CreatePrism(material, bounds.Footprint, height).Transform(Matrix4x4.CreateTranslation(0, height / 2f - bounds.Height / 2f + GroundHeight, 0));
            geometry.Union(prism);
        }
    }
}
