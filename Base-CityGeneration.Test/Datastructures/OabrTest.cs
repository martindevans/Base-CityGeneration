using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Utilities.SVG;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Extensions;
using Placeholder.ConstructiveSolidGeometry;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class OabrTest
    {
        [TestMethod]
        public void AssertThat_AllPointsLiesInBounds_WithRandomShape()
        {
            var rand = new Random();
            var points = new List<Vector2>();
            for (int i = 0; i < 20; i++)
                points.Add(rand.RandomNormalVector().XZ() * (float)rand.NextDouble() * 50);

            //Calculate OABR
            var oabr = OABR.Fit(points);

            //Draw it
            var svg = new SvgRenderer(2);
            var hull = points.ConvexHull().ToArray();
            svg.AddOutline(hull, "green");
                svg.AddOutline((IReadOnlyList<Vector2>)oabr.Points(new Vector2[4]));
            foreach (var vector2 in points)
                svg.AddCircle(vector2, 1, oabr.Contains(vector2) ? "red" : "blue");
            Console.WriteLine(svg.Render());

            //Assert
            Assert.IsTrue(points.All(p => oabr.Contains(p)));
        }
    }
}
