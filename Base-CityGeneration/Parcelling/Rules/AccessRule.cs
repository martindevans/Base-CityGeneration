using System;

namespace Base_CityGeneration.Parcelling.Rules
{
    public class AccessRule<T>
        : ITerminationRule<T>
        where T : class, IParcelElement<T>
    {
        private readonly string _resource;
        private readonly float _chance;

        public AccessRule(string resource, float chance)
        {
            _resource = resource;
            _chance = chance;
        }

        public float? TerminationChance(Parcel<T> parcel)
        {
            return null;
        }

        public bool Discard(Parcel<T> parcel, Func<double> random)
        {
            return !parcel.HasAccess(_resource) && random() <= _chance;

        }
    }
}
