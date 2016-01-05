using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    [ContractClass(typeof(BaseFootprintAlgorithmContracts))]
    public abstract class BaseFootprintAlgorithm
    {
        /// <summary>
        /// Apply this algorithm to the given footprint to generate a new footprint
        /// </summary>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="footprint">The result of the previous algorithm in the sequence</param>
        /// <param name="basis">The initial footprint which started the sequence</param>
        /// <param name="lot">The shape of the lot of the building</param>
        /// <returns>A new footprint, passed into the next in sequence as the "footprint" parameter</returns>
        public abstract IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot);

        internal abstract class BaseContainer
            : IUnwrappable<BaseFootprintAlgorithm>
        {
            public abstract BaseFootprintAlgorithm Unwrap();
        }
    }

    [ContractClassFor(typeof(BaseFootprintAlgorithm))]
    internal abstract class BaseFootprintAlgorithmContracts
        : BaseFootprintAlgorithm
    {
        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(footprint != null);
            Contract.Requires(basis != null);
            Contract.Requires(lot != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);

            return default(IReadOnlyList<Vector2>);
        }
    }
}
