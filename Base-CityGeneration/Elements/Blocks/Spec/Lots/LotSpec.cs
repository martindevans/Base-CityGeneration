using System;
using Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints;
using Base_CityGeneration.Parcels.Parcelling;
using Base_CityGeneration.Utilities;
using System.Collections.Generic;
using System.Linq;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots
{
    public class LotSpec
    {
        private readonly BaseLotConstraint[] _constraints;
        public IEnumerable<BaseLotConstraint> Constraints
        {
            get { return _constraints; }
        }

        private readonly KeyValuePair<float, string[]>[] _tags;
        public IEnumerable<KeyValuePair<float, string[]>> Tags
        {
            get { return _tags; }
        }

        private LotSpec(BaseLotConstraint[] constraints, KeyValuePair<float, string[]>[] tags)
        {
            _constraints = constraints;
            _tags = tags;
        }

        internal class BaseContainer
        {
            public BaseLotConstraint.BaseContainer[] Constraints { get; set; }

            public TagContainer Tags { get; set; }

            public LotSpec Unwrap()
            {
                return new LotSpec(
                    (Constraints ?? new BaseLotConstraint.BaseContainer[0]).Select(a => a.Unwrap()).ToArray(),
                    Tags.ToArray()
                );
            }
        }

        public bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            foreach (var constraint in _constraints)
                if (!constraint.Check(parcel, random, metadata))
                    return false;

            return true;
        }
    }
}
