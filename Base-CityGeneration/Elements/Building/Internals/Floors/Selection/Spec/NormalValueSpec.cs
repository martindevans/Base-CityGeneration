﻿
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

        private readonly bool _vary;
        public bool Vary { get { return _vary; } }

        private float? _singleCache;
        private int? _intCache; 

        public NormalValueSpec(float min, float mean, float max, float deviation, bool vary = false)
        {
            _min = min;
            _mean = mean;
            _max = max;
            _deviation = deviation;
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
            return MathHelper.Clamp(random.NormallyDistributedSingle(Deviation, Mean), Min, Max);
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
        {
            public float Min { get; set; }
            public float? Mean { get; set; }
            public float Max { get; set; }
            public float? Deviation { get; set; }
            public bool Vary { get; set; }

            /// <summary>
            /// Cache unwrapped value, so multiple unwraps all return exactly the same item
            /// </summary>
            internal NormalValueSpec Unwrapped { get; set; }
        }
    }

    internal static class NormalValueSpecContainerExtensions
    {
        public static NormalValueSpec Unwrap(this NormalValueSpec.Container container, Func<float, float, float> meanCalc = null, Func<float, float, float> deviationCalc = null)
        {
            if (container.Unwrapped == null)
            {
                meanCalc = meanCalc ?? MeanCalc;
                deviationCalc = deviationCalc ?? DeviationCalc;

                var min = container.Min;
                var max = container.Max;
                var mean = container.Mean.HasValue ? container.Mean.Value : meanCalc(min, max);
                var deviation = container.Deviation.HasValue ? container.Deviation.Value : deviationCalc(min, max);

                container.Unwrapped = new NormalValueSpec(min, mean, max, deviation, container.Vary);
            }

            return container.Unwrapped;
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
