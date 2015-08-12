
using System;
using Base_CityGeneration.Parcels.Parcelling;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules
{
    public abstract class BaseSubdividerRule
    {
        public abstract ITerminationRule Rule(Func<double> random);

        internal abstract class BaseContainer
        {
            public abstract BaseSubdividerRule Unwrap();
        }
    }
}
