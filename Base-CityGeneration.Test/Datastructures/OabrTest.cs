using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using EpimetheusPlugins.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Extensions;
using Placeholder.ConstructiveSolidGeometry;
using PrimitiveSvgBuilder;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class OabrTest
    {
        [TestMethod]
        public void AssertThat_AllPointsLiesInBounds_WithRandomShape()
        {
            //Generate a random load of points
            var rand = new Random();
            var points = new List<Vector2>();
            for (int i = 0; i < 20; i++)
                points.Add(rand.RandomNormalVector().XZ() * (float)rand.NextDouble() * 50);

            //Calculate OABR
            var oabr = OABR.Fit(points);

            //Draw it
            var svg = new SvgBuilder(2);
            var hull = points.ConvexHull().ToArray();
            svg.Outline(hull, "green");
            svg.Outline((IReadOnlyList<Vector2>)oabr.Points(new Vector2[4]));
            foreach (var vector2 in points)
                svg.Circle(vector2, 1, oabr.Contains(vector2) ? "red" : "blue");
            Console.WriteLine(svg.ToString());

            //Assert that every point lies within the bounds
            Assert.IsTrue(points.All(p => oabr.Contains(p)));
        }

        [TestMethod]
        public void AssertThat_FromWorld_ToWorld_AreIdentityFunction()
        {
            //Generate a random load of points
            var rand = new Random();
            var points = new List<Vector2>();
            for (int i = 0; i < 20; i++)
                points.Add(rand.RandomNormalVector().XZ() * (float)rand.NextDouble() * 50);

            //Calculate OABR
            var oabr = OABR.Fit(points);

            //Draw it
            var svg = new SvgBuilder(2);
            var hull = points.ConvexHull().ToArray();
            svg.Outline(hull, "green");
            svg.Outline((IReadOnlyList<Vector2>)oabr.Points(new Vector2[4]));
            foreach (var vector2 in points)
                svg.Circle(vector2, 1, oabr.Contains(vector2) ? "red" : "blue");
            Console.WriteLine(svg.ToString());

            //Assert that every point lies within the bounds
            foreach (var vector2 in points)
            {
                var trans = oabr.FromWorld(vector2);
                var world = oabr.ToWorld(trans);

                Assert.IsTrue(vector2.TolerantEquals(world, 0.001f), string.Format("Expected {0} and {1} to be equal", vector2, world));
            }
        }

        [TestMethod]
        public void AssertThat_CornerPoints_AreEqualToInputPoints_ForRectangularShape()
        {
            var points = new[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10),
            };

            var oabr = OABR.Fit(points);

            var corners = oabr.Points();

            for (var i = 0; i < points.Length; i++)
                Assert.AreEqual(points[i], corners[i]);
        }
    }
}
