
using System;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public abstract class BaseSubdividerRule
    {
        protected readonly BaseValueGenerator TerminationChance;

        protected BaseSubdividerRule(BaseValueGenerator terminationChance)
        {
            TerminationChance = terminationChance;
        }

        public abstract ITerminationRule Rule(Func<double> random);

        internal abstract class BaseContainer
        {
            // Making this protected breaks serialization
            // ReSharper disable once MemberCanBeProtected.Global
            public object TerminationChance { get; set; }

            public abstract BaseSubdividerRule Unwrap();
        }
    }
}
