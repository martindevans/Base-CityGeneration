using System;
using System.Diagnostics.Contracts;

namespace Base_CityGeneration.Parcels.Parcelling.Rules
{
    public class AccessRule
        : ITerminationRule
    {
        private readonly string _resource;
        private readonly float _chance;

        /// <summary>
        /// A rule which requires that parcels have access to a given resource
        /// </summary>
        /// <param name="resource">The required resource (accessible along at least one of the edges of the parcel)</param>
        /// <param name="chance">The chance that a parcel will *not* have access to this resource</param>
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
