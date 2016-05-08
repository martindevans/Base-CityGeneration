using System;
using System.Collections.Generic;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Throw
        : BaseFootprintAlgorithm
    {
        private readonly Func<Exception> _func;

        public Throw(Func<Exception> func)
        {
            _func = func;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            throw _func();
        }
    }
}
