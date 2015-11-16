using System;
using System.Linq;
using System.Numerics;
using System.Text;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Myre.Collections;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    public class BaseAlgorithmTest
    {
        public Vector2[] Test(BaseFootprintAlgorithm alg, Vector2[] shape, INamedDataCollection metadata = null)
        {
            var r = new Random(10);
            var result = alg.Apply(r.NextDouble, metadata ?? new NamedBoxCollection(), shape, shape, shape);

            //Check that result is clockwise wound
            //Assert.IsFalse(Clipper.Orientation(result.Select(a => new IntPoint((int)(a.X * 1000), (int)(a.Y * 1000))).ToList()));

            //Display result
            const float SCALE = 10;
            StringBuilder svg = new StringBuilder();
            svg.Append(string.Format("M {0} {1} ", result[0].X * SCALE, result[0].Y * SCALE));
            for (int i = 1; i < result.Count; i++)
                svg.Append(string.Format("L {0} {1} ", result[i].X * SCALE, result[i].Y * SCALE));
            svg.Append("Z");
            var min = result.Aggregate(Vector2.Min) * SCALE;
            Console.WriteLine("<svg width=\"500px\" height=\"500px\"><g transform=\"translate({1} {2})\"><path d=\"{0}\"></path></g></svg>", svg, -min.X, -min.Y);

            return result.ToArray();
        }
    }
}
