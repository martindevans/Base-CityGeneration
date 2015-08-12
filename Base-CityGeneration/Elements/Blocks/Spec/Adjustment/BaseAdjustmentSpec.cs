
using System;
using System.Collections.Generic;
using Base_CityGeneration.Parcels.Parcelling;

namespace Base_CityGeneration.Elements.Blocks.Spec.Adjustment
{
    public abstract class BaseAdjustmentSpec
    {
        public abstract IEnumerable<Parcel> Adjust(Parcel block, IEnumerable<Parcel> parcels, Func<double> random);

        public abstract class BaseContainer
        {
            public abstract BaseAdjustmentSpec Unwrap();
        }
    }
}
