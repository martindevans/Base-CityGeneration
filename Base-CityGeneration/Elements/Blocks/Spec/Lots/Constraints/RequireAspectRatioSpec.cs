using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;
using System;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    class RequireAspectRatioSpec
        : BaseLotConstraint
    {
        private readonly IValueGenerator _min;
        private readonly IValueGenerator _max;

        private RequireAspectRatioSpec(IValueGenerator min, IValueGenerator max)
        {
            Contract.Requires(min != null);
            Contract.Requires(max != null);

            _min = min;
            _max = max;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_min != null);
            Contract.Invariant(_max != null);
        }

        public override bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            var min = _min.SelectFloatValue(random, metadata);
            var max = _max.SelectFloatValue(random, metadata);
            var ratio = parcel.AspectRatio();

            return ratio >= min && ratio <= max;
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; [UsedImplicitly]set; }
            public object Max { get; [UsedImplicitly]set; }
            public object TerminationChance { get; set; }

            public override BaseLotConstraint Unwrap()
            {
                return new RequireAspectRatioSpec(
                    IValueGeneratorContainer.FromObject(Min ?? 0),
                    IValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity)
                );
            }
        }
    }
}
