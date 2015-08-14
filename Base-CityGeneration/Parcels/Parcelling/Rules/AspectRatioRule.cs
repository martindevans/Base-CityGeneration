using System;

namespace Base_CityGeneration.Parcels.Parcelling.Rules
{
    public class AspectRatioRule
        : ITerminationRule
    {
        private readonly float _max;
        private readonly float _min;
        private readonly float _terminationChance;

        public AspectRatioRule(float min, float max, float terminationChance)
        {
            _max = max;
            _min = min;
            _terminationChance = terminationChance;
        }

        public float? TerminationChance(Parcel parcel)
        {
            var ratio = parcel.AspectRatio();

            //If the ratio does not exceed min, there's a chance we'll terminate
            return ratio < _min
                ? (float?)null
                : _terminationChance;
        }

        public bool Discard(Parcel parcel, Func<double> random)
        {
            var ratio = parcel.AspectRatio();

            //If the ratio exceeds the max, discard this parcel
            return ratio > _max;
        }
    }
}
