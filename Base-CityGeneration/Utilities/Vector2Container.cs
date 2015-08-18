using Base_CityGeneration.Utilities.Numbers;
using Microsoft.Xna.Framework;
using System;
using Myre.Collections;

namespace Base_CityGeneration.Utilities
{
    internal class Vector2Container
    {
        public object X { get; set; }

        public object Y { get; set; }

        public Vector2 Unwrap(Func<double> random, INamedDataCollection metadata)
        {
            return new Vector2(
                BaseValueGeneratorContainer.FromObject(X).SelectFloatValue(random, metadata),
                BaseValueGeneratorContainer.FromObject(Y).SelectFloatValue(random, metadata)
            );
        }
    }
}
