using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    class FrontageRuleSpec
        : BaseSubdividerRule
    {
        private readonly BaseValueGenerator _min;
        private readonly BaseValueGenerator _max;
        private readonly BaseValueGenerator _terminationChance;
        private readonly string _resource;

        private FrontageRuleSpec(BaseValueGenerator min, BaseValueGenerator max, BaseValueGenerator terminationChance, string resource)
        {
            _min = min;
            _max = max;
            _terminationChance = terminationChance;
            _resource = resource;
        }

        public override ITerminationRule Rule(Func<double> random)
        {
            return new FrontageRule(
                _min.SelectFloatValue(random),
                _max.SelectFloatValue(random),
                _terminationChance.SelectFloatValue(random),
                _resource
            );
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; set; }
            public object Max { get; set; }
            public object TerminationChance { get; set; }
            public string Type { get; set; }

            public override BaseSubdividerRule Unwrap()
            {
                return new FrontageRuleSpec(
                    BaseValueGeneratorContainer.FromObject(Min ?? 0),
                    BaseValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity),
                    BaseValueGeneratorContainer.FromObject(TerminationChance ?? 0),
                    Type
                );
            }
        }
    }
}
