using EpimetheusPlugins.Procedural.Utilities;
using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Clip
        : BaseFootprintAlgorithm
    {
        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis)
        {
            Clipper c = new Clipper();

            const int SCALE = 1000;

            c.AddPolygon(footprint.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Y * SCALE))).ToList(), PolyType.Subject);
            c.AddPolygon(basis.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Y * SCALE))).ToList(), PolyType.Clip);

            List<List<IntPoint>> solutions = new List<List<IntPoint>>();
            c.Execute(ClipType.Intersection, solutions);

            var clipperSolution = solutions.Single();

            if (Clipper.Orientation(clipperSolution))
                clipperSolution.Reverse();

            return clipperSolution.Select(a => new Vector2(a.X / (float)SCALE, a.Y / (float)SCALE)).ToArray();
        }

        public class Container
            : BaseContainer
        {
            public object Angle { get; set; }

            internal override BaseFootprintAlgorithm Unwrap()
            {
                return new Clip();
            }
        }
    }
}
