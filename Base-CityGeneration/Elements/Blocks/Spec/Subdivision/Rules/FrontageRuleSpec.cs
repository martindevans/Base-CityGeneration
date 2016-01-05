using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class FrontageRuleSpec
        : BaseSubdividerRule
    {
        private readonly IValueGenerator _min;
        private readonly IValueGenerator _max;
        private readonly string _resource;

        private FrontageRuleSpec(IValueGenerator min, IValueGenerator max, IValueGenerator terminationChance, string resource)
            : base(terminationChance)
        {
            Contract.Requires(min != null);
            Contract.Requires(max != null);
            Contract.Requires(terminationChance != null);

            _min = min;
            _max = max;
            _resource = resource;
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_min != null);
            Contract.Invariant(_max != null);
        }

        public override ITerminationRule Rule(Func<double> random, INamedDataCollection metadata)
        {
            return new FrontageRule(
                _min.SelectFloatValue(random, metadata),
                _max.SelectFloatValue(random, metadata),
                TerminationChance.SelectFloatValue(random, metadata),
                _resource
            );
        }

        internal class Container
            : BaseContainer
        {
            public object Min { get; set; }
            public object Max { get; set; }
            public string Type { get; set; }

            public override BaseSubdividerRule Unwrap()
            {
                return new FrontageRuleSpec(
                    IValueGeneratorContainer.FromObject(Min ?? 0),
                    IValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity),
                    IValueGeneratorContainer.FromObject(TerminationChance ?? 0),
                    Type
                );
            }
        }
    }
}
