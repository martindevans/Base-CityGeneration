using Base_CityGeneration.Utilities.Numbers;
using System.Numerics;
using System;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Utilities
{
    internal class Vector2Container
    {
        public object X { get; [UsedImplicitly]set; }
        public object Y { get; [UsedImplicitly]set; }

        public Vector2 Unwrap(Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            return new Vector2(
                IValueGeneratorContainer.FromObject(X).SelectFloatValue(random, metadata),
                IValueGeneratorContainer.FromObject(Y).SelectFloatValue(random, metadata)
            );
        }
    }
}
