using System;
using Base_CityGeneration.Elements.Building;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building
{
    [TestClass]
    public class BaseBuildingTest
    {
        [TestMethod]
        public void FloorHeightCalculation()
        {
            Random r = new Random(2);

            int f;
            int b;
            float h;
            BaseBuilding.CalculateFloorHeight(r.NextDouble, 5, 20, 3, 5, 1, 10, 100, 20, out f, out b, out h);

            Assert.IsTrue(f <= 10);
            Assert.IsTrue(f >= 1);
            Assert.IsTrue(100 / h >= 5);
            Assert.IsTrue(100 / h <= 20);
            Assert.IsTrue(b <= 5);
            Assert.IsTrue(b >= 3);
        }

        [TestMethod]
        public void FloorHeightCalculationLimitsAtMaxFloors()
        {
            Random r = new Random(1);

            for (int i = 0; i < 100; i++)
            {
                int f;
                int b;
                float h;
                BaseBuilding.CalculateFloorHeight(r.NextDouble, 5, 20, 3, 5, 1, 10, 500, 200, out f, out b, out h);

                Assert.IsTrue(f >= 5);
                Assert.IsTrue(f <= 20);
                Assert.IsTrue(h >= 1);
                Assert.IsTrue(h <= 10);
                Assert.IsTrue(b <= 5);
                Assert.IsTrue(b >= 3);
            }
        }
    }
}
