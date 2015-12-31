using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class AccessRuleSpec
        : BaseSubdividerRule
    {
        private readonly string _resource;

        private AccessRuleSpec(IValueGenerator terminationChance, string resource)
            : base(terminationChance)
        {
            Contract.Requires<ArgumentNullException>(terminationChance != null, "terminationChance");

            _resource = resource;
        }

        public override ITerminationRule Rule(Func<double> random, INamedDataCollection metadata)
        {
            return new AccessRule(
                _resource,
                TerminationChance.SelectFloatValue(random, metadata)
            );
        }

        internal class Container
            : BaseContainer
        {
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
