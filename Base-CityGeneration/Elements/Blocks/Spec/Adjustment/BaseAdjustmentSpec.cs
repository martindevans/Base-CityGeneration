
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints;
using Base_CityGeneration.Parcels.Parcelling;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Adjustment
{
    public abstract class BaseAdjustmentSpec
    {
        private readonly BaseLotConstraint[] _selectors;

        protected BaseAdjustmentSpec(IEnumerable<BaseLotConstraint> selectors)
        {
            Contract.Requires(selectors != null);

            _selectors = selectors.ToArray();
        }

        public abstract IEnumerable<Parcel> Adjust(Parcel block, IEnumerable<Parcel> parcels, Func<double> random);

        protected IEnumerable<Parcel> SelectParcels(IEnumerable<Parcel> parcels, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(parcels != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Ensures(Contract.Result<IEnumerable<Parcel>>() != null);

            if (_selectors.Length == 0)
                return parcels;

            return parcels.Where(p => _selectors.All(s => s.Check(p, random, metadata)));
        }

        public abstract class BaseContainer
        {
            public BaseLotConstraint.BaseContainer[] Selectors { get; set; }

            public abstract BaseAdjustmentSpec Unwrap();
        }
    }
}
