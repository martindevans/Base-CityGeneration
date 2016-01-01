using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            Contract.Requires<ArgumentNullException>(area != null, "area != null");
            Contract.Requires<ArgumentNullException>(algorithm != null, "algorithm != null");

            Area = area;
        }

        protected override bool Condition(Func<double> random, Myre.Collections.INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis)
        {
            var area = Area.SelectFloatValue(random, metadata);

            const int SCALE = 1000;
            var measuredArea = Math.Abs(Clipper.Area(footprint.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Y * SCALE))).ToList())) / (SCALE * SCALE);

            return measuredArea > area;
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
