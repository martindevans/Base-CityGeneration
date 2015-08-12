﻿using System;
using Base_CityGeneration.Parcels.Parcelling;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public abstract class BaseLotConstraint
    {
        public abstract bool Check(Parcel parcel, Func<double> random);

        public abstract class BaseContainer
        {
            public abstract BaseLotConstraint Unwrap();
        }
    }
}
