using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class AccessRuleSpec
        : BaseSubdividerRule
    {
        private readonly BaseValueGenerator _terminationChance;
        private readonly string _resource;

        private AccessRuleSpec(BaseValueGenerator terminationChance, string resource)
        {
            _terminationChance = terminationChance;
            _resource = resource;
        }

        public override ITerminationRule Rule(Func<double> random)
        {
            return new AccessRule(
                _resource,
                _terminationChance.SelectFloatValue(random)
            );
        }

        internal class Container
            : BaseContainer
        {
            public object TerminationChance { get; set; }
            public string Type { get; set; }

            public override BaseSubdividerRule Unwrap()
            {
                return new AccessRuleSpec(
                    BaseValueGeneratorContainer.FromObject(TerminationChance ?? 0),
                    Type
                );
            }
        }
    }
}
