using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class AreaRuleSpec
        : BaseSubdividerRule
    {
        private readonly BaseValueGenerator _min;
        private readonly BaseValueGenerator _max;
        private readonly BaseValueGenerator _terminationChance;

        private AreaRuleSpec(BaseValueGenerator min, BaseValueGenerator max, BaseValueGenerator terminationChance)
        {
            _min = min;
            _max = max;
            _terminationChance = terminationChance;
        }

        public override ITerminationRule Rule(Func<double> random)
        {
            return new AreaRule(
                _min.SelectFloatValue(random),
                _max.SelectFloatValue(random),
                _terminationChance.SelectFloatValue(random)
            );
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; set; }
            public object Max { get; set; }
            public object TerminationChance { get; set; }

            public override BaseSubdividerRule Unwrap()
            {
                return new AreaRuleSpec(
                    BaseValueGeneratorContainer.FromObject(Min ?? 0),
                    BaseValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity),
                    BaseValueGeneratorContainer.FromObject(TerminationChance ?? 0)
                );
            }
        }
    }
}
