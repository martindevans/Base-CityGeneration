using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision
{
    public class ObbParcellerSpec
        : BaseSubdivideSpec
    {
        private readonly BaseValueGenerator _nonOptimalOabbChance;
        private readonly BaseValueGenerator _nonOptimalOabbMaxRatio;

        private readonly BaseValueGenerator _splitPointSelection;

        private readonly BaseSubdividerRule[] _rules;
        public override IEnumerable<BaseSubdividerRule> Rules
        {
            get { return _rules; }
        }

        public ObbParcellerSpec(BaseValueGenerator nonOptimalOabbChance, BaseValueGenerator nonOptimalOabbMaxRatio, BaseValueGenerator splitPointGenerator, BaseSubdividerRule[] rules)
        {
            _nonOptimalOabbChance = nonOptimalOabbChance;
            _nonOptimalOabbMaxRatio = nonOptimalOabbMaxRatio;
            _splitPointSelection = splitPointGenerator;

            _rules = rules;
        }

        public override IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random)
        {
            ObbParceller p = new ObbParceller();
            if (_nonOptimalOabbChance != null)
                p.NonOptimalOabbChance = _nonOptimalOabbChance.SelectFloatValue(random);
            if (_nonOptimalOabbMaxRatio != null)
                p.NonOptimalOabbMaxRatio = _nonOptimalOabbMaxRatio.SelectFloatValue(random);
            if (_splitPointSelection != null)
                p.SplitPointGenerator = _splitPointSelection;

            foreach (var rule in _rules)
                p.AddTerminationRule(rule.Rule(random));

            return p.GenerateParcels(root, random);
        }

        internal class Container
            : BaseContainer
        {
            public object NonOptimalChance { get; set; }
            public object MaxNonOptimalRatio { get; set; }

            public object SplitRatio { get; set; }

            public override BaseSubdivideSpec Unwrap()
            {
                return new ObbParcellerSpec(
                    NonOptimalChance == null ? null : BaseValueGeneratorContainer.FromObject(NonOptimalChance),
                    MaxNonOptimalRatio == null ? null : BaseValueGeneratorContainer.FromObject(MaxNonOptimalRatio),
                    SplitRatio == null ? null : BaseValueGeneratorContainer.FromObject(SplitRatio),
                    (Rules ?? new BaseSubdividerRule.BaseContainer[0]).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
    }
}
