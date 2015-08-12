using System.Linq;
using Base_CityGeneration.Parcels.Parcelling;
using System;
using System.Collections.Generic;
using EpimetheusPlugins.Procedural;

namespace Base_CityGeneration.Parcels.Adjusting
{
    /// <summary>
    /// Merge random adjacent lots to create more complex lot footprints
    /// </summary>
    public class MergeAdjacentLots
        : IParcelAdjuster
    {
        private readonly float _mergeChance;

        public MergeAdjacentLots(float mergeChance)
        {
            _mergeChance = mergeChance;
        }

        public IEnumerable<Parcel> Adjust(Parcel.Edge[] block, IEnumerable<Parcel> parcels, Func<double> random)
        {
            while (random() <= _mergeChance && _mergeChance > 0)
            {
                //Find all parcels which share an edge
                bool any;
                var adjacency = FindAdjacentParcels(parcels, out any);

                //If there are no pairs, break out
                if (!any)
                    break;

                //Select a pair
                var pair = adjacency.Random(random);

                //Calculate new parcel set after merging parcels
                parcels = Merge(parcels, pair.Key, pair.Value).ToArray();
            }

            return parcels;
        }

        private IEnumerable<Parcel> Merge(IEnumerable<Parcel> parcels, Parcel a, Parcel b)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<KeyValuePair<Parcel, Parcel>> FindAdjacentParcels(IEnumerable<Parcel> parcels, out bool any)
        {
            throw new NotImplementedException();
        }
    }
}
