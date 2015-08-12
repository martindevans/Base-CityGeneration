using System;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public class RequireAreaSpec
        : BaseLotConstraint
    {
        private readonly BaseValueGenerator _min;
        private readonly BaseValueGenerator _max;

        private RequireAreaSpec(BaseValueGenerator min, BaseValueGenerator max)
        {
            _min = min;
            _max = max;
        }

        public override bool Check(Parcel parcel, Func<double> random)
        {
            var min = _min.SelectFloatValue(random);
            var max = _max.SelectFloatValue(random);
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
                    BaseValueGeneratorContainer.FromObject(Min ?? 0),
                    BaseValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity)
                );
            }
        }
    }
}
