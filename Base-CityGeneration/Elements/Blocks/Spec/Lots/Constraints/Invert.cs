using Base_CityGeneration.Parcels.Parcelling;
using System;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public class Invert
        : BaseLotConstraint
    {
        private readonly BaseLotConstraint _inner;

        private Invert(BaseLotConstraint inner)
        {
            _inner = inner;
        }

        public override bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            return !_inner.Check(parcel, random, metadata);
        }

        internal class Container
            : BaseContainer
        {
            public BaseContainer Inner { get; set; }

            public override BaseLotConstraint Unwrap()
            {
                return new Invert(Inner.Unwrap());
            }
        }
    }
}
