using System;
using System.Collections.Generic;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public abstract class BaseFootprintAlgorithm
    {
        /// <summary>
        /// Apply this algorithm to the given footprint to generate a new footprint
        /// </summary>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="footprint">The result of the previous algorithm in the sequence</param>
        /// <param name="basis">The initial footprint which started the sequence</param>
        /// <returns>A new footprint, passed into the next in sequence as the "footprint" parameter</returns>
        public abstract IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis);

        public abstract class BaseContainer
        {
            internal abstract BaseFootprintAlgorithm Unwrap();
        }
    }
}
