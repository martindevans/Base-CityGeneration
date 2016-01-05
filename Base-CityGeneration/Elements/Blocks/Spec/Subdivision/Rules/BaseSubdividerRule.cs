
using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    [ContractClass(typeof(BaseSubdividerRuleContracts))]
    public abstract class BaseSubdividerRule
    {
        protected readonly IValueGenerator TerminationChance;

        protected BaseSubdividerRule(IValueGenerator terminationChance)
        {
            Contract.Requires(terminationChance != null);

            TerminationChance = terminationChance;
        }

        public abstract ITerminationRule Rule(Func<double> random, INamedDataCollection metadata);

        internal abstract class BaseContainer
            : IUnwrappable<BaseSubdividerRule>
        {
            // Making this protected breaks serialization
            // ReSharper disable once MemberCanBeProtected.Global
            public object TerminationChance { get; set; }

            public abstract BaseSubdividerRule Unwrap();
        }
    }

    [ContractClassFor(typeof(BaseSubdividerRule))]
    internal abstract class BaseSubdividerRuleContracts
        : BaseSubdividerRule
    {
        protected BaseSubdividerRuleContracts(IValueGenerator terminationChance)
            : base(terminationChance)
        {
        }

        public override ITerminationRule Rule(Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            return default(ITerminationRule);
        }
    }
}
