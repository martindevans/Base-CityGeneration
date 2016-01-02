using System;
using System.Diagnostics.Contracts;
using Myre;
using Myre.Collections;

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Base_CityGeneration.Styles
{
    public static class InternalWalls
    {
        /// <summary>
        /// The minimum thickness of a normal internal wall
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumInternalWallThicknessName = new TypedNameDefault<float>("wall_internal_thickness_min", 0.05f);

        /// <summary>
        /// The maximum thickness of a normal internal wall
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumInternalWallThicknessName = new TypedNameDefault<float>("wall_internal_thickness_max", 0.2f);

        /// <summary>
        /// The thickness of a normal internal wall
        /// </summary>
        public readonly static TypedName<float> InternalWallThicknessName = new TypedName<float>("wall_internal_thickness");

        public static float InternalWallThickness(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            Contract.Requires(provider != null);
            Contract.Requires(random != null);

            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, InternalWallThicknessName, MinimumInternalWallThicknessName, MaximumInternalWallThicknessName, min, max);
        }
    }
}
