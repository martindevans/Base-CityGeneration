
using System;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class NormalValueSpec
    {
        private readonly float _min;
        public float Min { get { return _min; } }

        private readonly float _mean;
        public float Mean { get { return _mean; } }

        private readonly float _max;
        public float Max { get { return _max; } }

        private readonly float _deviation;
        public float Deviation { get { return _deviation; } }

        private readonly string _group;
        public string Group { get { return _group; } }

        private readonly bool _vary;
        public bool Vary { get { return _vary; } }

        private float? _cache;

        public NormalValueSpec(float min, float mean, float max, float deviation, string group = null, bool vary = false)
        {
            _min = min;
            _mean = mean;
            _max = max;
            _deviation = deviation;
            _group = group;
            _vary = vary;
        }

        public float SelectFloatValue(Func<double> random, IGroupFinder groupFinder)
        {
            //Return value from cache (if there is one)
            if (_cache.HasValue)
                return _cache.Value;

            //Calculate a height, and cache it if necessary
            float h = CalculateValue(random, groupFinder);
            if (!Vary)
                _cache = h;

            //Done!
            return h;
        }

        public int SelectIntValue(Func<double> random, IGroupFinder groupFinder)
        {
            //Select a float value in the normal way
            var f = SelectFloatValue(random, groupFinder);

            //Rearrange the min and max to be integers (in a narrower or equal range)
            var min = (int)Math.Ceiling(Min);
            var max = (int)Math.Floor(Max);

            //If they're the same we don't have a whole lot of choice!
            if (min == max)
                return min;

            //If they're inverted the range is too narrow (e.g. Min:0.1 Max:0.9 we can't select any integers in that range)
            if (min > max)
                throw new InvalidOperationException(string.Format("Cannot select an integer between {0} and {1}", Min, Max));

            //Clamp and round the value
            return (int)Math.Round(MathHelper.Clamp(f, min, max), MidpointRounding.AwayFromZero);
        }

        private float CalculateValue(Func<double> random, IGroupFinder groupFinder)
        {
            if (string.IsNullOrWhiteSpace(_group))
                return MathHelper.Clamp(random.NormallyDistributedSingle(Deviation, Mean), Min, Max);

            return groupFinder.Find(_group).SelectFloatValue(random, groupFinder);
        }

        internal class Container
        {
            public float Min { get; set; }
            public float? Mean { get; set; }
            public float Max { get; set; }
            public float? Deviation { get; set; }
            public string Group { get; set; }
            public bool Vary { get; set; }
        }
    }

    internal static class NormalValueSpecContainerExtensions
    {
        public static NormalValueSpec Unwrap(this NormalValueSpec.Container container, Func<float, float, float> meanCalc = null, Func<float, float, float> deviationCalc = null)
        {
            meanCalc = meanCalc ?? MeanCalc;
            deviationCalc = deviationCalc ?? DeviationCalc;

            var min = container.Min;
            var max = container.Max;
            var mean = container.Mean.HasValue ? container.Mean.Value : meanCalc(min, max);
            var deviation = container.Deviation.HasValue ? container.Deviation.Value : deviationCalc(min, max);

            return new NormalValueSpec(min, mean, max, deviation, container.Group, container.Vary);
        }

        private static float MeanCalc(float min, float max)
        {
            return min * 0.5f + max * 0.5f;
        }

        private static float DeviationCalc(float min, float max)
        {
            return (max - min) * 0.2f;
        }
    }
}
