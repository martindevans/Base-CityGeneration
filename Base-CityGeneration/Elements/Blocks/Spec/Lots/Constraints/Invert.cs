using Base_CityGeneration.Parcels.Parcelling;
using System;

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

        public override bool Check(Parcel parcel, Func<double> random)
        {
            return !_inner.Check(parcel, random);
        }

        internal class Container
            : BaseLotConstraint.BaseContainer
        {
            public BaseLotConstraint.BaseContainer Inner { get; set; }

            public override BaseLotConstraint Unwrap()
            {
                return new Invert(Inner.Unwrap());
            }
        }
    }
}
