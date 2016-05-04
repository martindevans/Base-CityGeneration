using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using EpimetheusPlugins.Procedural;
using Myre;
using Myre.Collections;

using MathHelperRedux;

namespace Base_CityGeneration.Styles
{
    public static class InternalWalls
    {
        #region thickness
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
        #endregion

        #region material
        /// <summary>
        /// The default material of ceilings walls
        /// </summary>
        public static readonly TypedNameDefault<string> DefaultCeilingMaterialName = new TypedNameDefault<string>("ceiling_material_default", "concrete");

        public static string DefaultCeilingMaterial(this INamedDataCollection provider, Func<double> random, params string[] possibilities)
        {
            Contract.Requires(provider != null);
            Contract.Requires(random != null);
            Contract.Requires(possibilities != null);

            //Select a random value from the possibilities, if no possibilities are supplied use the default material
            var generated = possibilities.Length == 0 ? provider.DefaultMaterial(random) : possibilities[random.RandomInteger(0, possibilities.Length - 1)];

            return provider.DetermineHierarchicalValue(DefaultCeilingMaterialName, oldValue =>
            {
                //If no possibilities were provided, everything is valid!
                if (possibilities.Length == 0)
                    return oldValue;

                //Use the old value if it is one of the allowed possibilities
                if (((IList<string>)possibilities).Contains(oldValue))
                    return oldValue;

                //Otherwise generate a new value (from the range of allowed possibilities)
                return generated;
            }, () => generated);
        }
        #endregion
    }
}
