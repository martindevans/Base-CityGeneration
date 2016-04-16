using Myre.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using ClipperLib;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class Clip
        : BaseFootprintAlgorithm
    {
        private readonly bool _lot;

        public Clip(bool lot)
        {
            _lot = lot;
        }

        public override IReadOnlyList<Vector2> Apply(Func<double> random, INamedDataCollection metadata, IReadOnlyList<Vector2> footprint, IReadOnlyList<Vector2> basis, IReadOnlyList<Vector2> lot)
        {
            var c = new Clipper();

            const int SCALE = 1000;

            c.AddPath(footprint.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Y * SCALE))).ToList(), PolyType.ptSubject, true);

            var clip = _lot ? lot : basis;
            c.AddPath(clip.Select(a => new IntPoint((int)(a.X * SCALE), (int)(a.Y * SCALE))).ToList(), PolyType.ptClip, true);

            var solutions = new List<List<IntPoint>>();
            c.Execute(ClipType.ctIntersection, solutions);

            var clipperSolution = solutions.Single();
            Contract.Assume(clipperSolution != null);

            if (Clipper.Orientation(clipperSolution))
                clipperSolution.Reverse();

            return clipperSolution.Select(a => new Vector2(a.X / (float)SCALE, a.Y / (float)SCALE)).ToArray();
        }

        internal class Container
            : BaseContainer
        {
            public bool Lot { get; [UsedImplicitly]set; }

            public override BaseFootprintAlgorithm Unwrap()
            {
                return new Clip(Lot);
            }
        }
    }
}
