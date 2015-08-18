using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;
using System;
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
            _min = min;
            _max = max;
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
            public object Min { get; set; }
            public object Max { get; set; }
            public object TerminationChance { get; set; }

            public override BaseLotConstraint Unwrap()
            {
                return new RequireAspectRatioSpec(
                    BaseValueGeneratorContainer.FromObject(Min ?? 0),
                    BaseValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity)
                );
            }
        }
    }
}
