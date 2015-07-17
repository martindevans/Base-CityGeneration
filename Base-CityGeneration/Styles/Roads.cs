using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;
using System;

namespace Base_CityGeneration.Styles
{
    public static class Roads
    {
        /// <summary>
        /// The minimum width of a road lane
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumLaneWidthName = new TypedNameDefault<float>("road_lane_width_min", 2.7f);

        /// <summary>
        /// The maximum width of a road lane
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumLaneWidthName = new TypedNameDefault<float>("road_lane_width_max", 3.7f);

        /// <summary>
        /// The width of a road lane
        /// </summary>
        public readonly static TypedName<float> RoadLaneWidthName = new TypedName<float>("road_lane_width");

        /// <summary>
        /// Get the road lane width, or if one has not been generated, generate one based off MinimumLaneWidthName and MaximumLaneWidthName value
        /// </summary>
        /// <param name="provider">The provider to get and put value from/to</param>
        /// <param name="random">A random number generator (generating values from 0 to 1)</param>
        /// <param name="min">The minimum allowable value</param>
        /// <param name="max">The maximum allowable value</param>
        /// <returns></returns>
        public static float RoadLaneWidth(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, RoadLaneWidthName, MinimumLaneWidthName, MaximumLaneWidthName, min, max);
        }

        /// <summary>
        /// The minimum width of a sidewalk
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumSidewalkWidthName = new TypedNameDefault<float>("road_sidewalk_width_min", 1.8f);

        /// <summary>
        /// The maximum width of a sidewalk
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumSidewalkWidthName = new TypedNameDefault<float>("road_sidewalk_width_max", 3.1f);

        /// <summary>
        /// The width of a sidewalk
        /// </summary>
        public readonly static TypedName<float> RoadSidewalkWidthName = new TypedName<float>("road_sidewalk_width");

        /// <summary>
        /// Get the sidewalk width, or if one has not been generated, generate one based off MinimumSidewalkWidthName and MaximumSidewalkWidthName value
        /// </summary>
        /// <param name="provider">The provider to get and put value from/to</param>
        /// <param name="random">A random number generator (generating values from 0 to 1)</param>
        /// <param name="min">The minimum allowable value</param>
        /// <param name="max">The maximum allowable value</param>
        /// <returns></returns>
        public static float RoadSidewalkWidth(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, RoadSidewalkWidthName, MinimumSidewalkWidthName, MaximumSidewalkWidthName, min, max);
        }
    }
}
