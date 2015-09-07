using System.Collections.Generic;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public abstract class BaseFootprintAlgorithm
    {
        public abstract IReadOnlyList<Vector2> Apply(IReadOnlyList<Vector2> footprint);

        internal abstract class BaseContainer
        {
            internal abstract BaseFootprintAlgorithm Unwrap();
        }
    }
}
