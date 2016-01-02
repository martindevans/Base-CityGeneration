using Myre;
using Myre.Collections;
using System;
using System.Diagnostics.Contracts;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

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
            Contract.Requires(provider != null);
            Contract.Requires(random != null);

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
            Contract.Requires(provider != null);
            Contract.Requires(random != null);

            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, RoadSidewalkWidthName, MinimumSidewalkWidthName, MaximumSidewalkWidthName, min, max);
        }


        /// <summary>
        /// The minimum Height of a sidewalk
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumSidewalkHeightName = new TypedNameDefault<float>("road_sidewalk_height_min", 0.1f);

        /// <summary>
        /// The maximum Height of a sidewalk
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumSidewalkHeightName = new TypedNameDefault<float>("road_sidewalk_height_max", 0.25f);

        /// <summary>
        /// The Height of a sidewalk
        /// </summary>
        public readonly static TypedName<float> RoadSidewalkHeightName = new TypedName<float>("road_sidewalk_height");

        /// <summary>
        /// Get the sidewalk Height, or if one has not been generated, generate one based off MinimumSidewalkHeightName and MaximumSidewalkHeightName value
        /// </summary>
        /// <param name="provider">The provider to get and put value from/to</param>
        /// <param name="random">A random number generator (generating values from 0 to 1)</param>
        /// <param name="min">The minimum allowable value</param>
        /// <param name="max">The maximum allowable value</param>
        /// <returns></returns>
        public static float RoadSidewalkHeight(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            Contract.Requires(provider != null);
            Contract.Requires(random != null);

            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, RoadSidewalkHeightName, MinimumSidewalkHeightName, MaximumSidewalkHeightName, min, max);
        }


        /// <summary>
        /// Default material for road sidewalks
        /// </summary>
        public static readonly TypedNameDefault<string> DefaultSidewalkMaterialName = new TypedNameDefault<string>("road_sidewalk_default_material", "concrete");

        /// <summary>
        /// Material for sidewalks
        /// </summary>
        public static readonly TypedName<string> RoadSidewalkMaterialName = new TypedName<string>("road_sidewalk_material");

        /// <summary>
        /// Get the sidewalk material, or if one has not been generated, generate one based off DefaultSidewalkMaterial
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="random"></param>
        /// <param name="defaultMaterial"></param>
        /// <returns></returns>
        public static string RoadSidewalkMaterial(this INamedDataCollection provider, Func<double> random, string defaultMaterial = null)
        {
            Contract.Requires(provider != null);
            Contract.Requires(random != null);

            return provider.DefaultMaterial(random, RoadSidewalkMaterialName, defaultMaterial ?? provider.GetValue(DefaultSidewalkMaterialName));
        }
    }
}
