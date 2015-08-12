using System;
using System.Collections.Generic;
using Base_CityGeneration.Parcels.Adjusting;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities.Numbers;

namespace Base_CityGeneration.Elements.Blocks.Spec.Adjustment
{
    public class MergeAdjacentSpec
        : BaseAdjustmentSpec
    {
        private readonly BaseValueGenerator _chance;

        private MergeAdjacentSpec(BaseValueGenerator chance)
        {
            _chance = chance;
        }

        public override IEnumerable<Parcel> Adjust(Parcel block, IEnumerable<Parcel> parcels, Func<double> random)
        {
            var merge = new MergeAdjacentLots(_chance.SelectFloatValue(random));

            return merge.Adjust(block.Edges, parcels, random);
        }

        internal class Container
            : BaseContainer
        {
            public object Chance { get; set; }

            public override BaseAdjustmentSpec Unwrap()
            {
                return new MergeAdjacentSpec(
                    BaseValueGeneratorContainer.FromObject(Chance)
                );
            }
        }
    }
}
