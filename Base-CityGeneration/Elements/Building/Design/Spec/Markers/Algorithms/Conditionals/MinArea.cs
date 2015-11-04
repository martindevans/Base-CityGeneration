using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural.Utilities;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms.Conditionals
{
    public class MinArea
        : BaseConditional
    {
        public IValueGenerator Area { get; private set; }

        public MinArea(IValueGenerator area, BaseFootprintAlgorithm algorithm)
            : base(algorithm)
        {
            Area = area;
        }

        protected override bool Condition(Func<double> random, Myre.Collections.INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis)
        {
            var area = Area.SelectFloatValue(random, metadata);

            return Clipper.Area(footprint.Select(a => new IntPoint((int)(a.X * 1000), (int)(a.Y * 1000))).ToList()) > area;
        }

        public class Container
            : BaseConditionalContainer
        {
            public object Area { get; set; }

            internal override BaseFootprintAlgorithm Unwrap()
            {
                return new MinArea(BaseValueGeneratorContainer.FromObject(Area), Action.Unwrap());
            }
        }
    }
}
