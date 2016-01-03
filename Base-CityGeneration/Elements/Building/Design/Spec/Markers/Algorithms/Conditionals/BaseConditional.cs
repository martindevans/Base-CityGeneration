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

        protected BaseConditional(BaseFootprintAlgorithm action)
        {
            Contract.Requires(action != null);

            _action = action;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            //Apply action
            var result = _action.Apply(random, metadata, footprint, basis, lot);

            //check that result is acceptable
            if (Condition(random, metadata, result, basis))
                return result;

            //Result was unacceptable, return input
            return footprint;
        }

        protected abstract bool Condition(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis);

        public abstract class BaseConditionalContainer
           : BaseContainer
        {
            public BaseContainer Action { get; set; }
        }
    }
}
