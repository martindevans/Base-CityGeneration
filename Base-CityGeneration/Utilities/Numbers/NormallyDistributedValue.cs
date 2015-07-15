using System;
using EpimetheusPlugins.Procedural;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Utilities.Numbers
{
    public class NormallyDistributedValue
        : BaseValueGenerator
    {
        private readonly float _mean;
        public float Mean
        {
            get
            {
                return _mean;
            }
        }

        private readonly float _deviation;
        public float Deviation
        {
            get
            {
                return _deviation;
            }
        }

        private float? _singleCache;

        public NormallyDistributedValue(float min, float mean, float max, float deviation, bool vary = false)
            : base(min, max, vary)
        {
            _mean = mean;
            _deviation = deviation;
        }

        protected override float GenerateFloatValue(Func<double> random)
        {
            return MathHelper.Clamp(random.NormallyDistributedSingle(Deviation, Mean), Min, Max);
        }

        internal class Container
            : BaseValueGeneratorContainer
        {
            public float Min { get; set; }
            public float? Mean { get; set; }
            public float Max { get; set; }
            public float? Deviation { get; set; }
            public bool Vary { get; set; }

            protected override BaseValueGenerator UnwrapImpl()
            {

                var mean = Mean.HasValue ? Mean.Value : MeanCalc(Min, Max);
                var deviation = Deviation.HasValue ? Deviation.Value : DeviationCalc(Min, Max);

                return new NormallyDistributedValue(Min, mean, Max, deviation, Vary);
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
}
