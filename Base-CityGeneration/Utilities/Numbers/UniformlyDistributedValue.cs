using System;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class UniformlyDistributedValue
        : IValueGenerator
    {
        private readonly float _min;
        public float Min { get { return _min; } }

        private readonly float _max;
        public float Max { get { return _max; } }

        private readonly bool _vary;
        public bool Vary { get { return _vary; } }

        private float? _singleCache;
        private int? _intCache;

        public UniformlyDistributedValue(float min, float max, bool vary = false)
        {
            _min = min;
            _max = max;
            _vary = vary;
        }

        public float SelectFloatValue(Func<double> random)
        {
            if (_singleCache.HasValue)
                return _singleCache.Value;

            var f = GenerateFloatValue(random);
            if (!Vary)
                _singleCache = f;
            return f;
        }

        private float GenerateFloatValue(Func<double> random)
        {
            return random.RandomSingle(Min, Max);
        }

        public int SelectIntValue(Func<double> random)
        {
            if (_intCache.HasValue)
                return _intCache.Value;

            var i = GenerateIntValue(random);
            if (!Vary)
                _intCache = i;
            return i;
        }

        private int GenerateIntValue(Func<double> random)
        {
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
            return (int)Math.Round(MathHelper.Clamp(GenerateFloatValue(random), min, max), MidpointRounding.AwayFromZero);
        }

        internal class Container
            : IValueGeneratorContainer
        {
            public float Min { get; set; }
            public float Max { get; set; }
            public bool Vary { get; set; }

            IValueGenerator IValueGeneratorContainer.Unwrapped { get; set; }

            IValueGenerator IValueGeneratorContainer.Unwrap()
            {
                var self = (IValueGeneratorContainer)this;
                if (self.Unwrapped == null)
                    self.Unwrapped = new UniformlyDistributedValue(Min, Max, Vary);

                return self.Unwrapped;
            }
        }
    }
}
