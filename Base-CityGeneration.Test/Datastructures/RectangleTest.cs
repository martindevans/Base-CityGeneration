using Base_CityGeneration.Datastructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class RectangleTest
    {
        [TestMethod]
        public void RectangleIntersection()
        {
            var rectA = new RectangleF(10, 10, 20, 20);
            var rectB = new RectangleF(20, 20, 30, 30);
            var rectC = new RectangleF(70, 70, 20, 20);

            Assert.IsTrue(rectA.Intersects(rectB));
            Assert.IsFalse(rectA.Intersects(rectC));
        }
    }
}
