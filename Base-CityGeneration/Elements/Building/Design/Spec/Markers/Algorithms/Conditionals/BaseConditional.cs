using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Numerics;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms.Conditionals
{
    public abstract class BaseConditional
        : BaseFootprintAlgorithm
    {
        private readonly BaseFootprintAlgorithm _action;
        private readonly BaseFootprintAlgorithm _fallback;

        protected BaseConditional(BaseFootprintAlgorithm action, BaseFootprintAlgorithm fallback)
        {
            Contract.Requires(action != null);

            _action = action ?? new Identity();
            _fallback = fallback ?? new Identity();
        }

        [ContractInvariantMethod]
        private void ContractInvariants()
        {
            Contract.Invariant(_action != null);
            Contract.Invariant(_fallback != null);
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            //Apply action
            var result = _action.Apply(random, metadata, footprint, basis, lot);

            //check that result is acceptable
            if (Condition(random, metadata, result, basis))
                return result;

            //Result is unacceptable. Apply fallback
            return _fallback.Apply(random, metadata, footprint, basis, lot);
        }

        protected abstract bool Condition(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis);

        internal abstract class BaseConditionalContainer
           : BaseContainer
        {
            public BaseContainer Action { get; set; }
            public BaseContainer Fallback { get; set; }
        }
    }
}
