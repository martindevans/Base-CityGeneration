
using System;
using Microsoft.Xna.Framework;
using Myre;
using Myre.Collections;

namespace Base_CityGeneration.Styles
{
    public static class Doors
    {
        /// <summary>
        /// The minimum width of a normal sized door
        /// </summary>
        public static readonly TypedNameDefault<float> MinimumStandardDoorWidthName = new TypedNameDefault<float>("door_width_min", 0.9f);

        /// <summary>
        /// The maximum width of a normal sized door
        /// </summary>
        public static readonly TypedNameDefault<float> MaximumStandardDoorWidthName = new TypedNameDefault<float>("door_width_max", 2f);

        /// <summary>
        /// The width of a normal door
        /// </summary>
        public readonly static TypedName<float> StandardDoorWidthName = new TypedName<float>("door_width");

        public static float StandardDoorWidth(this INamedDataCollection provider, Func<double> random, float? min = null, float? max = null)
        {
            return provider.DetermineHierarchicalValue(random, MathHelper.Lerp, StandardDoorWidthName, MinimumStandardDoorWidthName, MaximumStandardDoorWidthName, min, max);
        }
    }
}
