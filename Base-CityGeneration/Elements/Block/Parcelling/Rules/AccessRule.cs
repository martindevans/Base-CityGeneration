using System;

namespace Base_CityGeneration.Elements.Block.Parcelling.Rules
{
    public class AccessRule
        :ITerminationRule
    {
        private readonly string _resource;
        private readonly float _chance;

        public AccessRule(string resource, float chance)
        {
            _resource = resource;
            _chance = chance;
        }

        public float? TerminationChance(Parcel parcel)
        {
            return null;
        }

        public bool Discard(Parcel parcel, Func<double> random)
        {
            return !parcel.HasAccess(_resource) && random() <= _chance;

        }
    }
}
