using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    [TestClass]
    public class InvertCornerTest
        : BaseAlgorithmTest
    {
        [TestMethod]
        public void AssertThat_InvertCorner_ReducesArea()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var r = Test(new InvertCorner(new ConstantValue(90), new ConstantValue(2), true, true), input);

            //We've slice off 4 corners of known area, so we know the exact target area
            Assert.AreEqual(400 - (4 * 4),  r.Area(), 0.01f);
        }

        [TestMethod]
        public void AssertThat_InvertCorner_DoesNotAffectCornersAboveThreshold()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            //Set angle threshold low enough that no bevelling will occur
            var r = Test(new InvertCorner(new ConstantValue(45), new ConstantValue(2), true, true), input);

            for (var i = 0; i < input.Length; i++)
                Assert.AreEqual(input[i], r[i]);
        }

        [TestMethod]
        public void AssertThat_InvertCorner_DoesNotExceedEdgeLength()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            //Set angle threshold low enough that no bevelling will occur
            var r = Test(new InvertCorner(new ConstantValue(90), new ConstantValue(50), true, true), input);

            //We've sliced away the entire floor!
            Assert.AreEqual(0, r.Area(), 0.01f);
        }
    }
}
