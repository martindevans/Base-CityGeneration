
using System;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec
{
    public class HeightSpec
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

        public HeightSpec(float min, float mean, float max, float deviation, string group, bool vary)
        {
            _min = min;
            _mean = mean;
            _max = max;
            _deviation = deviation;
            _group = group;
            _vary = vary;
        }

        public float SelectHeight(Func<double> random, IGroupFinder groupFinder)
        {
            //Return value from cache (if there is one)
            if (_cache.HasValue)
                return _cache.Value;

            //Calculate a height, and cache it if necessary
            float h = CalculateHeight(random, groupFinder);
            if (!Vary)
                _cache = h;

            //Done!
            return h;
        }

        private float CalculateHeight(Func<double> random, IGroupFinder groupFinder)
        {
            if (string.IsNullOrWhiteSpace(_group))
                return MathHelper.Clamp(random.NormallyDistributedSingle(Deviation, Mean), Min, Max);

            return groupFinder.Find(_group).SelectHeight(random, groupFinder);
        }

        internal class Container
        {
            public float? Min { get; set; }
            public float? Mean { get; set; }
            public float? Max { get; set; }
            public float? Deviation { get; set; }
            public string Group { get; set; }
            public bool Vary { get; set; }
        }
    }

    internal static class HeightSpecContainerExtensions
    {
        public static HeightSpec Unwrap(this HeightSpec.Container container)
        {
            if (container == null)
                container = new HeightSpec.Container();

            var min = container.Min ?? 2.5f;
            var max = container.Max ?? 3.5f;
            var mean = container.Mean ?? (min * 0.5f + max * 0.5f);
            var deviation = container.Deviation ?? (max - min) * 0.2f;

            return new HeightSpec(min, mean, max, deviation, container.Group, container.Vary);
        }
    }
}
