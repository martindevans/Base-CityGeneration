﻿using System;
using Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots
{
    public class LotSpec
    {
        private readonly BaseLotConstraint[] _constraints;
        public IEnumerable<BaseLotConstraint> Constraints
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<BaseLotConstraint>>() != null);
                return _constraints;
            }
        }

        private readonly KeyValuePair<float, KeyValuePair<string, string>[]>[] _tags;
        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>>>() != null);
                return _tags;
            }
        }

        private LotSpec(BaseLotConstraint[] constraints, KeyValuePair<float, KeyValuePair<string, string>[]>[] tags)
        {
            Contract.Requires(constraints != null);
            Contract.Requires(tags != null);

            _constraints = constraints;
            _tags = tags;
        }

        internal class BaseContainer
        {
            public BaseLotConstraint.BaseContainer[] Constraints { get; [UsedImplicitly]set; }
            public TagContainerContainer Tags { get; [UsedImplicitly]set; }

            public LotSpec Unwrap()
            {
                Contract.Requires(Tags != null);

                return new LotSpec(
                    (Constraints ?? new BaseLotConstraint.BaseContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    Tags.Unwrap().ToArray()
                );
            }
        }

        public bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(parcel != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            return _constraints.All(constraint => constraint.Check(parcel, random, metadata));
        }
    }
}
