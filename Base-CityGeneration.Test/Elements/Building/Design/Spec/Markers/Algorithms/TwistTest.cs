using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    [TestClass]
    class TwistTest
        : BaseAlgorithmTest
    {
        [TestMethod]
        public void AssertThat_TwistFootprint_DoesNotChangeArea()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var r = Test(new Twist(new ConstantValue(10)), input);

            Assert.AreEqual(20 * 20, r.Area(), 0.01f);
        }
    }
}
