using System;
using System.Collections.Generic;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Identity
        : BaseFootprintAlgorithm
    {
        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            return footprint;
        }

        internal class Container
            : BaseContainer
        {
            public override BaseFootprintAlgorithm Unwrap()
            {
                return new Identity();
            }
        }
    }
}
