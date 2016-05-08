using System;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Myre.Collections;
using PrimitiveSvgBuilder;

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
            Console.WriteLine(new SvgBuilder(10).Outline(result));

            return result.ToArray();
        }
    }
}
