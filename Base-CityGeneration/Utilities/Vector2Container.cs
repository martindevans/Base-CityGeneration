using Base_CityGeneration.Utilities.Numbers;
using Microsoft.Xna.Framework;
using System;

namespace Base_CityGeneration.Utilities
{
    internal class Vector2Container
    {
        public object X { get; set; }

        public object Y { get; set; }

        public Vector2 Unwrap(Func<double> random)
        {
            return new Vector2(
                BaseValueGeneratorContainer.FromObject(X).SelectFloatValue(random),
                BaseValueGeneratorContainer.FromObject(Y).SelectFloatValue(random)
            );
        }
    }
}
