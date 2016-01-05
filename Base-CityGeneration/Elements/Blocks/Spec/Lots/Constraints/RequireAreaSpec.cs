using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public class RequireAreaSpec
        : BaseLotConstraint
    {
        private readonly IValueGenerator _min;
        private readonly IValueGenerator _max;

        private RequireAreaSpec(IValueGenerator min, IValueGenerator max)
        {
            Contract.Requires(min != null);
            Contract.Requires(max != null);

            _min = min;
            _max = max;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_min != null);
            Contract.Invariant(_max != null);
        }

        public override bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            var min = _min.SelectFloatValue(random, metadata);
            var max = _max.SelectFloatValue(random, metadata);
            var area = parcel.Area();

            return area >= min && area <= max;
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; set; }
            public object Max { get; set; }
            public object TerminationChance { get; set; }

            public override BaseLotConstraint Unwrap()
            {
                return new RequireAreaSpec(
                    IValueGeneratorContainer.FromObject(Min ?? 0),
                    IValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity)
                );
            }
        }
    }
}
