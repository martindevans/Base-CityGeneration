using Base_CityGeneration.Parcels.Parcelling;
using System;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public class Invert
        : BaseLotConstraint
    {
        private readonly BaseLotConstraint _inner;

        private Invert(BaseLotConstraint inner)
        {
            Contract.Requires(inner != null);

            _inner = inner;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_inner != null);
        }

        public override bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            return !_inner.Check(parcel, random, metadata);
        }

        internal class Container
            : BaseContainer
        {
            public BaseContainer Inner { get; [UsedImplicitly]set; }

            public override BaseLotConstraint Unwrap()
            {
                Contract.Assume(Inner != null);

                return new Invert(Inner.Unwrap());
            }
        }
    }
}
