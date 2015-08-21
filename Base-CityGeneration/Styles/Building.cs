using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using System;

namespace Base_CityGeneration.Styles
{
    public static class Building
    {
        /// <summary>
        /// The minimum maximum height of a building
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumMaximumBuildingHeightName = new TypedNameDefault<float>("building_height_max_min", 2.1f);

        /// <summary>
        /// The maximum maximum height of a building
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumMaximumBuildingHeightName = new TypedNameDefault<float>("building_height_max_max", 3.5f);

        /// <summary>
        /// The maximum height of a building
        /// </summary>
        public readonly static TypedName<float> MaximumBuildingHeightName = new TypedName<float>("building_height_max");

        /// <summary>
        /// Get the maximum height of a building, or if one has not been generated, generate one based off MinimumMaximumBuildingHeightName and MaximumMaximumBuildingHeightName value
        /// </summary>
        /// <param name="provider">The provider to get and put value from/to</param>
        /// <param name="random">A random number generator (generating values from 0 to 1)</param>
        /// <param name="min">The minimum allowable value</param>
        /// <param name="max">The maximum allowable value</param>
        /// <returns></returns>
        public static float MaximumBuildingHeight(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, MaximumBuildingHeightName, MinimumMaximumBuildingHeightName, MaximumMaximumBuildingHeightName, min, max);
        }
    }
}
