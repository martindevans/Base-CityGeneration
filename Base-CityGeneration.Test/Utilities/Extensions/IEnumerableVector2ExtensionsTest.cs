using System.Linq;
using System.Numerics;
using Base_CityGeneration.Utilities.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Utilities.Extensions
{
    [TestClass]
    public class IEnumerableVector2ExtensionsTest
    {
        [TestMethod]
        public void AssertThat_Segments_ReturnsEmpty_WithEmpty()
        {
            var segments = new Vector2[0].Segments().ToArray();

            Assert.AreEqual(0, segments.Length);
        }

        [TestMethod]
        public void AssertThat_Segments_ReturnsZeroLengthSegment_WithSingle()
        {
            var segments = new Vector2[] { new Vector2(10, 20) }.Segments().ToArray();

            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(new Vector2(10, 20), segments.Single().Start);
            Assert.AreEqual(new Vector2(10, 20), segments.Single().End);
        }

        [TestMethod]
        public void AssertThat_Segments_ReturnsSegments_WithMultipleVectors()
        {
            var segments = new Vector2[] {
                new Vector2(10, 20),
                new Vector2(20, 20),
                new Vector2(20, 10),
                new Vector2(10, 10),
            }.Segments().ToArray();

            Assert.AreEqual(4, segments.Length);

            Assert.AreEqual(new Vector2(10, 20), segments[0].Start);
            Assert.AreEqual(new Vector2(20, 20), segments[0].End);

            Assert.AreEqual(new Vector2(20, 20), segments[1].Start);
            Assert.AreEqual(new Vector2(20, 10), segments[1].End);

            Assert.AreEqual(new Vector2(20, 10), segments[2].Start);
            Assert.AreEqual(new Vector2(10, 10), segments[2].End);

            Assert.AreEqual(new Vector2(10, 10), segments[3].Start);
            Assert.AreEqual(new Vector2(10, 20), segments[3].End);
        }
    }
}
