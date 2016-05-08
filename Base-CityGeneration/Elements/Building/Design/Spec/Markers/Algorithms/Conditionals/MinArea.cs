using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Utilities.Numbers;
using ClipperLib;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms.Conditionals
{
    public class MinArea
        : BaseConditional
    {
        public IValueGenerator Area { get; private set; }

        public MinArea(IValueGenerator area, BaseFootprintAlgorithm algorithm, BaseFootprintAlgorithm fallback)
            : base(algorithm, fallback)
        {
            Contract.Requires(area != null);
            Contract.Requires(algorithm != null);

            Area = area;
        }

        protected override bool Condition(Func<double> random, Myre.Collections.INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis)
        {
            var area = Area.SelectFloatValue(random, metadata);

            const int SCALE = 1000;
            var measuredArea = Math.Abs(Clipper.Area(footprint.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Y * SCALE))).ToList())) / (SCALE * SCALE);

            return measuredArea > area;
        }

        internal class Container
            : BaseConditionalContainer
        {
            public object Area { get; set; }

            public override BaseFootprintAlgorithm Unwrap()
            {
                return new MinArea(IValueGeneratorContainer.FromObject(Area), Action.Unwrap(), Fallback.UnwrapNullable());
            }
        }
    }
}
