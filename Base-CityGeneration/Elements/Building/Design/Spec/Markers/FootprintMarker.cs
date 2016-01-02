using System;
using System.Diagnostics.Contracts;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using System.Linq;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers
{
    public class FootprintMarker
        : BaseMarker
    {
        public FootprintMarker(BaseFootprintAlgorithm[] algorithms)
            : base(algorithms)
        {
            Contract.Requires(algorithms != null, "algorithms != null");
        }

        internal class Container
            : BaseContainer
        {
            protected override BaseFloorSelector Unwrap()
            {
                return new FootprintMarker(this.Select(a => a.Unwrap()).ToArray());
            }
        }
    }
}
