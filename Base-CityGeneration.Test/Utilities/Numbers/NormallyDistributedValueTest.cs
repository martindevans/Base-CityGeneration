using System;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Collections;

namespace Base_CityGeneration.Test.Utilities.Numbers
{
    [TestClass]
    public class NormallyDistributedValueTest
    {
        [TestMethod]
        public void AssertThat_SelectFloatValue_IsWithinRange()
        {
            NormallyDistributedValue spec = new NormallyDistributedValue(9.5f, 20, 30.5f, 10);

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var v = spec.SelectFloatValue(r.NextDouble, new NamedBoxCollection());

                Assert.IsTrue(v >= 9.5);
                Assert.IsTrue(v <= 30.5);
            }
        }

        [TestMethod]
        public void AssertThat_SelectIntValue_IsWithinRange()
        {
            NormallyDistributedValue spec = new NormallyDistributedValue(9.5f, 20, 30.5f, 10);

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var v = spec.SelectIntValue(r.NextDouble, new NamedBoxCollection());

                Assert.IsTrue(v >= 9.5);
                Assert.IsTrue(v <= 30.5);
            }
        }
    }
}
