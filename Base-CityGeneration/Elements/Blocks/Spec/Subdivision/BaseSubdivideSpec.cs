
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Blocks.Spec.Subdivision.Rules;
using Base_CityGeneration.Parcels.Parcelling;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Subdivision
{
    [ContractClass(typeof(BaseSubdivideSpecContracts))]
    public abstract class BaseSubdivideSpec
    {
        public abstract IEnumerable<BaseSubdividerRule> Rules { get; }

        public abstract IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata);

        internal abstract class BaseContainer
            : IUnwrappable<BaseSubdivideSpec>
        {
            // Making this protected breaks serialization
            // ReSharper disable once MemberCanBeProtected.Global
            public BaseSubdividerRule.BaseContainer[] Rules { get; set; }

            public abstract BaseSubdivideSpec Unwrap();
        }
    }

    [ContractClassFor(typeof(BaseSubdivideSpec))]
    internal abstract class BaseSubdivideSpecContracts
        : BaseSubdivideSpec
    {
        public override IEnumerable<BaseSubdividerRule> Rules
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseSubdividerRule>>() != null);
                return default(IEnumerable<BaseSubdividerRule>);
            }
        }

        public override IEnumerable<Parcel> GenerateParcels(Parcel root, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(root != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            return default(IEnumerable<Parcel>);
        }
    }
}
