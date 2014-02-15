using Base_CityGeneration.Datastructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class SeparatingAxisTesterTest
    {
        [TestMethod]
        public void OverlappingShapesOverlap()
        {
            var s1 = new Vector2[]
            {
                new Vector2(-5, -5),
                new Vector2(5, -5),
                new Vector2(5, 5),
                new Vector2(-5, 5)
            };

            var s2 = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(10, 0),
                new Vector2(10, 10),
                new Vector2(0, 10)
            };

            Assert.IsTrue(SeparatingAxisTester.Intersects(s1, s2));
        }

        [TestMethod]
        public void DisjointShapesDoNotOverlap()
        {
            var s1 = new Vector2[]
            {
                new Vector2(-5, -5),
                new Vector2(5, -5),
                new Vector2(5, 5),
                new Vector2(-5, 5)
            };

            var s2 = new Vector2[]
            {
                new Vector2(10, 0),
                new Vector2(20, 0),
                new Vector2(20, 10),
                new Vector2(10, 10)
            };

            Assert.IsFalse(SeparatingAxisTester.Intersects(s1, s2));
        }
    }
}
