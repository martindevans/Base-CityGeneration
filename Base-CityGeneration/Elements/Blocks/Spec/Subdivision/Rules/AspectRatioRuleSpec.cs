using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class AspectRatioRuleSpec
        : BaseSubdividerRule
    {
        private readonly IValueGenerator _maxRatio;
        private readonly IValueGenerator _minRatio;

        private AspectRatioRuleSpec(IValueGenerator terminationChance, IValueGenerator minRatio, IValueGenerator maxRatio)
            : base(terminationChance)
        {
            _maxRatio = maxRatio;
            _minRatio = minRatio;
        }

        public override ITerminationRule Rule(Func<double> random, INamedDataCollection metadata)
        {
            return new AspectRatioRule(
                _minRatio.SelectFloatValue(random, metadata),
                _maxRatio.SelectFloatValue(random, metadata),
                TerminationChance.SelectFloatValue(random, metadata)
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
