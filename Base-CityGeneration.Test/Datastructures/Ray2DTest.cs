using Base_CityGeneration.Datastructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class Ray2DTest
    {
        [TestMethod]
        public void Intersection()
        {
            var a = new Ray2D(Vector2.Zero, new Vector2(1, 0));
            var b = new Ray2D(new Vector2(1, 1), new Vector2(0, 1));

            float t;
            Assert.AreEqual(new Vector2(1, 0), a.Intersection2D(b, out t));

            Assert.AreEqual(1, t);
        }
    }
}
