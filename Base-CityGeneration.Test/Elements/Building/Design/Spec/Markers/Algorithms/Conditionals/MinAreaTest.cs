using System;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms;
using Base_CityGeneration.Elements.Building.Design.Spec.Markers.Algorithms.Conditionals;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Design.Spec.Markers.Algorithms.Conditionals
{
    [TestClass]
    public class MinAreaTest
        : BaseAlgorithmTest
    {
        [TestMethod]
        public void AssertThat_MinArea_RunsActionWhenAreaIsSufficient()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            Test(new MinArea(
                new ConstantValue(1),
                new Identity(),
                new Throw(() => new InvalidOperationException())
            ), input);
        }

        [TestMethod]
        public void AssertThat_MinArea_RunsFallbackWhenAreaIsNotSufficient()
        {
            var input = new Vector2[] {
                new Vector2(10, 10),
                new Vector2(10, -10),
                new Vector2(-10, -10),
                new Vector2(-10, 10)
            };

            var a = false;
            var b = false;

            Test(new MinArea(
                new ConstantValue(1000),
                new Act(() => { a = true; }), 
                new Act(() => { b = true; })
            ), input);

            Assert.IsTrue(a);   //Primary action ran, condition failed
            Assert.IsTrue(b);   //Fallback ran because primary failed
        }
    }
}
