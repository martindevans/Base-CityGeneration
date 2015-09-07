using System.Collections.Generic;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class FillSpace
        : BaseFootprintAlgorithm
    {
        public override IReadOnlyList<Vector2> Apply(IReadOnlyList<Vector2> footprint)
        {
            return footprint;
        }

        internal class Container
            : BaseContainer
        {
            internal override BaseFootprintAlgorithm Unwrap()
            {
                return new FillSpace();
            }
        }
    }
}
