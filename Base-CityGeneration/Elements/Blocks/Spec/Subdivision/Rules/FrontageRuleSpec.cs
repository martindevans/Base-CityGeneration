using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Parcels.Parcelling.Rules;
using Base_CityGeneration.Utilities.Numbers;
using System;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    class FrontageRuleSpec
        : BaseSubdividerRule
    {
        private readonly IValueGenerator _min;
        private readonly IValueGenerator _max;
        private readonly string _resource;

        private FrontageRuleSpec(IValueGenerator min, IValueGenerator max, IValueGenerator terminationChance, string resource)
            : base(terminationChance)
        {
            _min = min;
            _max = max;
            _resource = resource;
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
                    BaseValueGeneratorContainer.FromObject(Min ?? 0),
                    BaseValueGeneratorContainer.FromObject(Max ?? float.PositiveInfinity),
                    BaseValueGeneratorContainer.FromObject(TerminationChance ?? 0),
                    Type
                );
            }
        }
    }
}
