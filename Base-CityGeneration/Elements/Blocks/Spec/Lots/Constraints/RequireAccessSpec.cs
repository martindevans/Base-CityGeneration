using System;
using Base_CityGeneration.Parcels.Parcelling;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Blocks.Spec.Lots.Constraints
{
    public class RequireAccessSpec
        : BaseLotConstraint
    {
        private readonly string _type;

        private RequireAccessSpec(string type)
        {
            _type = type;
        }

        public override bool Check(Parcel parcel, Func<double> random, INamedDataCollection metadata)
        {
            return parcel.HasAccess(_type);
        }

        internal class Container
            : BaseContainer
        {
            public string Type { get; set; }

            public override BaseLotConstraint Unwrap()
            {
                return new RequireAccessSpec(Type);
            }
        }
    }
}
