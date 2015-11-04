using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    [TestClass]
    class BevelTest
        : BaseAlgorithmTest
    {
        [TestMethod]
        public void AssertThat_Bevel_ReducesArea()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var r = Test(new Bevel(new ConstantValue(90), new ConstantValue(2)), input);

            //We've slice off 4 triangle of known area, so we know the exact target area
            Assert.AreEqual(400 - (4 * 2),  r.Area(), 0.01f);
        }

        [TestMethod]
        public void AssertThat_Bevel_DoesNotAffectCornersAboveThreshold()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            //Set angle threshold low enough that no bevelling will occur
            var r = Test(new Bevel(new ConstantValue(45), new ConstantValue(2)), input);

            for (var i = 0; i < input.Length; i++)
                Assert.AreEqual(input[i], r[i]);
        }

        [TestMethod]
        public void AssertThat_Bevel_DoesNotExceedEdgeLength()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            //Set angle threshold low enough that no bevelling will occur
            var r = Test(new Bevel(new ConstantValue(90), new ConstantValue(50)), input);

            //We've slice off 4 triangle of known area, so we know the exact target area
            Assert.AreEqual(400 - (4 * 50), r.Area(), 0.01f);
        }
    }
}
