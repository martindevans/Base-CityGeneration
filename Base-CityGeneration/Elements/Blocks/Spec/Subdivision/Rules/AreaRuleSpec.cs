using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;
using System.Diagnostics.Contracts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public class AreaRuleSpec
        : BaseSubdividerRule
    {
        private readonly IValueGenerator _min;
        private readonly IValueGenerator _max;

        private AreaRuleSpec(IValueGenerator min, IValueGenerator max, IValueGenerator terminationChance)
            : base(terminationChance)
        {
            Contract.Requires(min != null);
            Contract.Requires(max != null);
            Contract.Requires(terminationChance != null);

            _min = min;
            _max = max;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(_min != null);
            Contract.Invariant(_max != null);
        }

        public override ITerminationRule Rule(Func<double> random, INamedDataCollection metadata)
        {
            return new AreaRule(
                _min.SelectFloatValue(random, metadata),
                _max.SelectFloatValue(random, metadata),
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
                return new AreaRuleSpec(
                    IValueGeneratorContainer.FromObject(Min ?? 0),
                    IValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity),
                    IValueGeneratorContainer.FromObject(TerminationChance ?? 0)
                );
            }
        }
    }
}
