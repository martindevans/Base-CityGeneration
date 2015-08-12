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
        IEnumerable<Parcel> Adjust(Parcel.Edge[] block, IEnumerable<Parcel> parcels, Func<double> random);
    }
}
