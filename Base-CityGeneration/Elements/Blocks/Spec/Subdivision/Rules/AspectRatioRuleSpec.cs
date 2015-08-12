using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class AspectRatioRuleSpec
        : BaseSubdividerRule
    {
        private readonly BaseValueGenerator _maxRatio;
        private readonly BaseValueGenerator _minRatio;

        private AspectRatioRuleSpec(BaseValueGenerator terminationChance, BaseValueGenerator minRatio, BaseValueGenerator maxRatio)
            : base(terminationChance)
        {
            _maxRatio = maxRatio;
            _minRatio = minRatio;
        }

        public override ITerminationRule Rule(Func<double> random)
        {
            return new AspectRatioRule(
                _minRatio.SelectFloatValue(random),
                _maxRatio.SelectFloatValue(random),
                TerminationChance.SelectFloatValue(random)
            );
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; set; }
            public object Max { get; set; }

            public override BaseSubdividerRule Unwrap()
            {
                return new AspectRatioRuleSpec(
                    BaseValueGeneratorContainer.FromObject(TerminationChance ?? 0),
                    BaseValueGeneratorContainer.FromObject(Min ?? 0),
                    BaseValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity)
                );
            }
        }
    }
}
