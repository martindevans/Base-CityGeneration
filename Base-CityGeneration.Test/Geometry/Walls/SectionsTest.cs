using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Geometry.Walls;
using Base_CityGeneration.Utilities.Extensions;
using EpimetheusPlugins.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrimitiveSvgBuilder;

namespace Base_CityGeneration.Test.Geometry.Walls
{
    [TestClass]
    public class SectionsTest
    {
        private static SvgBuilder Draw(IReadOnlyList<Section> sections, IReadOnlyList<IReadOnlyList<Vector2>> corners = null, IReadOnlyList<Vector2> outline = null, float scale = 30)
        {
            var svg = new SvgBuilder(scale);

            foreach (var section in sections)
            {
                svg.Outline(new[] {
                    section.Inner1, section.Inner2, section.Outer1, section.Outer2
                }, "black", "grey");
            }

            if (corners != null)
            {
                foreach (var corner in corners)
                {
                    svg.Outline(corner, "black", "darkgrey");
                }
            }

            //Draw corner sections vertices
            if (corners != null)
            {
                foreach (var corner in corners)
                    foreach (var vertex in corner)
                        svg.Circle(vertex, 5f / scale, "black");
            }

            //Draw wall section vertices
            foreach (var vertex in sections.SelectMany(s => new[] { s.Inner1, s.Inner2, s.Outer1, s.Outer2 }).Distinct())
                svg.Circle(vertex, 5f / scale, "black");

            //Draw outline
            if (outline != null)
                svg.Outline(outline, "red");

            return svg;
        }

        private static SvgBuilder Draw(IEnumerable<Vector2> shape, float scale = 30)
        {
            var svg = new SvgBuilder(scale);

            svg.Outline(shape.ToArray());

            return svg;
        }

        private static void TestSection(Vector2[] outline, Section section)
        {
            //Ensure all sections have 4 unique corners
            Assert.IsTrue(new[] {
                section.Inner1, section.Inner2, section.Outer1, section.Outer2
            }.GroupBy(a => a).Count() == 4);

            //Ensure outer points do in fact lie on the outside
            var o1D = outline.Segments().Select(s => s.DistanceToPoint(section.Outer1)).ToArray();
            Assert.IsTrue(o1D.Any(a => a < 0.01f));

            var o2D = outline.Segments().Select(s => s.DistanceToPoint(section.Outer2)).ToArray();
            Assert.IsTrue(o2D.Any(a => a < 0.01f));

            //Check winding
            Assert.IsTrue(section.GetCorners().IsClockwise());
        }

        [TestMethod]
        public void AssertThat_WallsSection_GeneratesExpectedSections_WithConvexRoom()
        {
            var shape = new[] { new Vector2(0, 0), new Vector2(0, 10), new Vector2(10, 10), new Vector2(10, 0) };

            Vector2[] inner;
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = shape.Sections(1, out inner, out corners).ToArray();

            Console.WriteLine(Draw(sections, corners, shape));

            //Test section properties
            foreach (var section in sections)
                TestSection(shape, section);

            Assert.AreEqual(4, sections.Length);
        }

        [TestMethod]
        public void AssertThat_WallsSection_GeneratesExpectedSections_WithConcaveRoom()
        {
            var shape = new[] {
                new Vector2(0, 0),
                new Vector2(0, 10),
                new Vector2(15, 10),
                new Vector2(15, 5),
                new Vector2(10, 5),
                new Vector2(10, 0)
            };

            Vector2[] inner;
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = shape.Sections(1, out inner, out corners).ToArray();
            Console.WriteLine(Draw(sections, corners, shape));

            //Ensure all sections have 4 unique corners
            Assert.IsTrue(sections.All(s => new[] {
                s.Inner1, s.Inner2, s.Outer1, s.Outer2
            }.GroupBy(a => a).Count() == 4));

            Assert.AreEqual(6, corners.Count);
            Assert.AreEqual(6, sections.Length);
        }

        [TestMethod]
        public void RegressionTest_KnownBadGeometry_CausesMatchUpToThrow()
        {
            //This specific shape caused Walls.Sections to throw:
            //
            // > "Cannot match up arrays with different lengths"
            //
            // This was because the Miter Limit on shrinking (only applicable when *growing*) was set to 0
            // This meant sometimes we squared off corners and, naturally, produced totally different shapes!
            var shape = new Vector2[] {
                new Vector2(15.01f, -4.976f),
                new Vector2(15.01f, -4.562f),
                new Vector2(15.423f, -4.562f),
                new Vector2(15.385f, -4.6f)
            };

            //This line used to throw
            Vector2[] inner;
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = shape.Sections(0.075f, out inner, out corners).ToArray();

            Console.WriteLine(Draw(sections, corners, shape, 1000));

            Assert.AreEqual(3, sections.Length);
        }

        [TestMethod]
        public void RegressionTest_KnownBadGeometry2_CausesMatchUpToThrow()
        {
            //This specific shape caused Walls.Sections to throw:
            //
            // > "Cannot match up arrays with different lengths"
            //
            // This was because of mitering limit (now set to int.MaxValue)

            var shape = new Vector2[] {
                new Vector2(27.406f, -9.14f),
                new Vector2(24.715f, -6.449f),
                new Vector2(26.356f, -4.808f),
                new Vector2(26.277f, -4.617f),
                new Vector2(26.404f, -4.617f),
                new Vector2(26.739f, -4.953f),
                new Vector2(27.073f, -4.617f),
                new Vector2(27.791f, -4.617f),
                new Vector2(27.791f, -5.464f),
                new Vector2(29.793f, -5.464f),
                new Vector2(29.793f, -5.465f),
                new Vector2(28.878f, -6.381f),
                new Vector2(29.52f, -7.025f)
            };

            //This line used to throw
            Vector2[] inner;
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = shape.Sections(0.175f, out inner, out corners).ToArray();

            Console.WriteLine(Draw(sections, corners, shape, 200));

            Assert.IsTrue(sections.Length <= 13);
        }

        [TestMethod]
        public void RegressionTest_KnownBadGeometry3_CausesParallelLineFindingToFail()
        {
            var shape = new[] {
                new Vector2(-70.999f, -49.99f),
                new Vector2(-99.99f, -49.99f),
                new Vector2(-99.99f, 49.99f),
                new Vector2(-70.999f, 49.99f),
                new Vector2(-70.999f, 10f),
                new Vector2(-69.01f, 10f),
                new Vector2(-69.01f, -10f),
                new Vector2(-70.999f, -10f)
            };

            Vector2[] inner;
            //This throws - cannot find a matching parallel section
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = shape.Sections(5, out inner, out corners).ToArray();

            Console.WriteLine(Draw(sections, corners, shape, 4));
        }

        [TestMethod]
        public void RegressionTest_KnownBadGeometry4_CausesMergedCorners()
        {
            var shape = new[] {
                new Vector2(-70, -75),
                new Vector2(0, -25),
                new Vector2(-50, -25),


                new Vector2(-50, 25),
                new Vector2(0, 25),
                new Vector2(0, 75),

                new Vector2(50, 75),
                new Vector2(50, -75),
            };

            Vector2[] inner;
            //This throws - cannot find a matching parallel section
            IReadOnlyList<IReadOnlyList<Vector2>> corners;
            var sections = shape.Sections(5, out inner, out corners).ToArray();

            Console.WriteLine(Draw(sections, corners, shape, 4));

            Assert.AreEqual(8, sections.Length);
            Assert.AreEqual(8, corners.Count);
        }
    }
}
