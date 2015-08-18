using System;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Utilities.Numbers
{
    [TestClass]
    public class ConstantValueTest
    {
        [TestMethod]
        public void AssertThat_SelectFloatValue_IsWithinRange()
        {
            ConstantValue spec = new ConstantValue(56.7f);

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var v = spec.SelectFloatValue(r.NextDouble, null);

                Assert.AreEqual(56.7f, v);
            }
        }

        [TestMethod]
        public void AssertThat_SelectIntValue_IsWithinRange()
        {
            ConstantValue spec = new ConstantValue(56.7f);

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var v = spec.SelectIntValue(r.NextDouble, null);

                Assert.AreEqual(57, v);
            }
        }
    }
}
