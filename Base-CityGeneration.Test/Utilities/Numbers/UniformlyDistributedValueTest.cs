﻿using System;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Myre.Collections;

namespace Base_CityGeneration.Test.Utilities.Numbers
{
    [TestClass]
    public class UniformlyDistributedValueTest
    {
        [TestMethod]
        public void AssertThat_SelectFloatValue_IsWithinRange()
        {
            UniformlyDistributedValue spec = new UniformlyDistributedValue(9.5f, 30.5f);

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
            UniformlyDistributedValue spec = new UniformlyDistributedValue(9.5f, 30.5f);

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
