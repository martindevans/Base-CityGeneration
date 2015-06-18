using Base_CityGeneration.Elements.Building.Internals.Floors.Selection.Spec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection.Spec
{
    [TestClass]
    public class NormalValueSpecTest
    {
        [TestMethod]
        public void AssertThat_SelectFloatValue_IsWithinRange()
        {
            NormalValueSpec spec = new NormalValueSpec(9.5f, 20, 30.5f, 10, null, true);

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var v = spec.SelectFloatValue(r.NextDouble, null);

                Assert.IsTrue(v >= 9.5);
                Assert.IsTrue(v <= 30.5);
            }
        }

        [TestMethod]
        public void AsserThat_SelectIntValue_IsWithinRange()
        {
            NormalValueSpec spec = new NormalValueSpec(9.5f, 20, 30.5f, 10, null, true);

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var v = spec.SelectIntValue(r.NextDouble, null);

                Assert.IsTrue(v >= 9.5);
                Assert.IsTrue(v <= 30.5);
            }
        }
    }
}
