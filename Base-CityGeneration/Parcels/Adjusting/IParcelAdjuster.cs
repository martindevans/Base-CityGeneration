using System;
using System.Collections.Generic;
using Base_CityGeneration.Parcels.Parcelling;

namespace Base_CityGeneration.Parcels.Adjusting
{
    /// <summary>
    /// Given a block and a set of land parcels, adjust the parcels
    /// </summary>
    public interface IParcelAdjuster
    {
        /// <summary>
        /// Adjust a set of parcels (a subset of all parcels)
        /// </summary>
        /// <param name="block">The block these parcels occupy</param>
        /// <param name="allParcels">All parcels in the block</param>
        /// <param name="workingSet">The working set of parcels to adjust</param>
        /// <param name="random">An RNG</param>
        /// <returns>A new set of *all* parcels</returns>
        IEnumerable<Parcel> Adjust(Parcel.Edge[] block, IEnumerable<Parcel> allParcels, IEnumerable<Parcel> workingSet, Func<double> random);
    }
}
