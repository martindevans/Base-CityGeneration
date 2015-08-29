using System.Linq;
using Base_CityGeneration.Datastructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class PathTest
    {
        [TestMethod]
        public void ConstructPath()
        {
            Path p = new Path(
                new Path.Segment(new Vector2(0, 0), 10),
                new Path.Segment(new Vector2(10, 0), 20)
            );

            Assert.AreEqual(1, p.Quadrangles.Count());

            var q1 = p.Quadrangles.First();

            Assert.IsTrue(q1.Contains(new Vector2(0, 5)));
            Assert.IsTrue(q1.Contains(new Vector2(0, -5)));
            Assert.IsTrue(q1.Contains(new Vector2(10, 10)));
            Assert.IsTrue(q1.Contains(new Vector2(10, -10)));
        }
    }
}
