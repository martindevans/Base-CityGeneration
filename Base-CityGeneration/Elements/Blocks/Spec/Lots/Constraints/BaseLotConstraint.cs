using System;
using Base_CityGeneration.Parcels.Parcelling;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public abstract class BaseLotConstraint
    {
        public abstract bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata);

        public abstract class BaseContainer
        {
            public abstract BaseLotConstraint Unwrap();
        }
    }
}
