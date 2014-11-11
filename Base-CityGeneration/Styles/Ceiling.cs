using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Styles
{
    public static class Ceiling
    {
        /// <summary>
        /// The minimum height of a normal ceiling
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumStandardCeilingHeightName = new TypedNameDefault<float>("ceiling_height_min", 2.1f);

        /// <summary>
        /// The maximum height of a normal ceiling
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumStandardCeilingHeightName = new TypedNameDefault<float>("ceiling_height_max", 3.5f);

        /// <summary>
        /// The height of a normal ceiling
        /// </summary>
        public readonly static TypedName<float> StandardCeilingHeightName = new TypedName<float>("ceiling_height");

        /// <summary>
        /// Get the standard ceiling height, or if one has not been generated, generate one based off MinimumStandardCeilingHeightName and MaximumStandardCeilingHeightName value
        /// </summary>
        /// <param name="provider">The provider to get and put value from/to</param>
        /// <param name="random">A random number generator (generating values from 0 to 1)</param>
        /// <param name="min">The minimum allowable value</param>
        /// <param name="max">The maximum allowable value</param>
        /// <returns></returns>
        public static float StandardCeilingHeight(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, StandardCeilingHeightName, MinimumStandardCeilingHeightName, MaximumStandardCeilingHeightName, min, max);
        }
    }
}
