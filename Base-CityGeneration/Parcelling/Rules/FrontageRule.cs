using System;

namespace Base_CityGeneration.Parcelling.Rules
{
    public class FrontageRule<T>
        : ITerminationRule<T>
        where T : class, IParcelElement<T>
    {
        private readonly float _hardMinFrontage;
        private readonly float _maxFrontage;
        private readonly float _terminationChance;
        private readonly string _resource;

        public FrontageRule(float hardMinFrontage, float maxFrontage, float terminationChance, string resource)
        {
            _hardMinFrontage = hardMinFrontage;
            _maxFrontage = maxFrontage;
            _terminationChance = terminationChance;
            _resource = resource;
        }

        public float? TerminationChance(Parcel<T> parcel)
        {
            return parcel.MaxAccessFrontage(_resource) < _maxFrontage ? _terminationChance : 0;
        }

        public bool Discard(Parcel<T> parcel, Func<double> random)
        {
            return parcel.MinAccessFrontage(_resource) < _hardMinFrontage;
        }
    }
}
