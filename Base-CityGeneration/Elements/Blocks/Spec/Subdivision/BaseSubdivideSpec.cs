
using System;
using System.Collections.Generic;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Parcels.Parcelling;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision
{
    public abstract class BaseSubdivideSpec
    {
        public abstract IEnumerable<BaseSubdividerRule> Rules { get; }

        public abstract IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata);

        internal abstract class BaseContainer
        {
            // Making this protected breaks serialization
            // ReSharper disable once MemberCanBeProtected.Global
            public BaseSubdividerRule.BaseContainer[] Rules { get; set; }

            public abstract BaseSubdivideSpec Unwrap();
        }
    }
}
