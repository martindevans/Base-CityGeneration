using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rectangle = Base_CityGeneration.Datastructures.Rectangle;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class RectangleTest
    {
        [TestMethod]
        public void RectangleIntersection()
        {
            var rectA = new Rectangle(10, 10, 20, 20);
            var rectB = new Rectangle(20, 20, 30, 30);
            var rectC = new Rectangle(70, 70, 20, 20);

            Assert.IsTrue(rectA.Intersects(rectB));
            Assert.IsFalse(rectA.Intersects(rectC));
        }
    }
}
