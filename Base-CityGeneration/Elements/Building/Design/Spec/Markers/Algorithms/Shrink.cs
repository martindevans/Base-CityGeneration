using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural.Utilities;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Shrink
        : BaseFootprintAlgorithm
    {
        private readonly IValueGenerator _distance;

        public Shrink(IValueGenerator distance)
        {
            _distance = distance;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            var amount = _distance.SelectFloatValue(random, metadata);

            return footprint.Shrink(amount).ToArray();
        }

        public class Container
            : BaseContainer
        {
            public object Distance { get; set; }

            internal override BaseFootprintAlgorithm Unwrap()
            {
                return new Shrink(
                    BaseValueGeneratorContainer.FromObject(Distance ?? 0)
                );
            }
        }
    }
}
