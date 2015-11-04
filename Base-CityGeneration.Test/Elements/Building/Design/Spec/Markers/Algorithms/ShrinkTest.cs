using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwizzleMyVectors;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms
{
    [TestClass]
    class ShrinkTest
        : BaseAlgorithmTest
    {
        [TestMethod]
        public void AssertThat_ShrinkFootprint_ReducesArea()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var r = Test(new Shrink(new ConstantValue(1)), input);

            Assert.AreEqual(18 * 18, r.Area());
        }
    }
}
