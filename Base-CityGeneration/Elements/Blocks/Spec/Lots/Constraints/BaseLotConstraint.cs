using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Parcels.Parcelling;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    [ContractClass(typeof(BaseLotConstraintContracts))]
    public abstract class BaseLotConstraint
    {
        public abstract bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata);

        public abstract class BaseContainer
            : IUnwrappable<BaseLotConstraint>
        {
            public abstract BaseLotConstraint Unwrap();
        }
    }

    [ContractClassFor(typeof(BaseLotConstraint))]
    internal abstract class BaseLotConstraintContracts
        : BaseLotConstraint
    {
        public override bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            return default(bool);
        }
    }
}
