using System;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.Edges;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Datastructures.Edges
{
    [TestClass]
    public class NeighbourSetTest
    {
        private readonly NeighbourSet _set = new NeighbourSet();

        [TestInitialize]
        public void Initialize()
        {
        }

        [TestMethod]
        public void AssertThat_AddEdges_IncreasesCount()
        {
            var r = new Random(123);

            for (var i = 0; i < 3000; i++)
            {
                _set.Add(new LineSegment2D(
                    new Vector2(RandomUtilities.RandomSingle(r.NextDouble, -100, 100), RandomUtilities.RandomSingle(r.NextDouble, -100, 100)),
                    new Vector2(RandomUtilities.RandomSingle(r.NextDouble, -100, 100), RandomUtilities.RandomSingle(r.NextDouble, -100, 100))
                ));

                Assert.AreEqual(i + 1, _set.Count);
            }
        }

        [TestMethod]
        public void AssertThat_OverlappingLineSegments_AreNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)));
            _set.Add(new LineSegment2D(new Vector2(5, 0), new Vector2(15, 0)));

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(1, neighbours.Length);
            Assert.AreEqual(new LineSegment2D(new Vector2(5, 0), new Vector2(15, 0)), neighbours.Single().Segment);
            Assert.AreEqual(0f, neighbours.Single().OverlapStart);
            Assert.AreEqual(0.5f, neighbours.Single().OverlapEnd);
        }

        [TestMethod]
        public void AssertThat_OverlappingLineSegments_AreNeighbours_WhenSegmentIsReversed()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)));
            _set.Add(new LineSegment2D(new Vector2(15, 0), new Vector2(5, 0)));

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(1, neighbours.Length);
            Assert.AreEqual(new LineSegment2D(new Vector2(15, 0), new Vector2(5, 0)), neighbours.Single().Segment);
            Assert.AreEqual(0.5f, neighbours.Single().OverlapStart);
            Assert.AreEqual(1.0f, neighbours.Single().OverlapEnd);
        }

        [TestMethod]
        public void AssertThat_ParallelButNotColinnearSegments_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)));
            _set.Add(new LineSegment2D(new Vector2(15, 10), new Vector2(5, 10)));

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }

        [TestMethod]
        public void AssertThat_ParallelButNearlyColinnearSegments_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)));
            _set.Add(new LineSegment2D(new Vector2(5, 0), new Vector2(15, 1f)));

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0.1f).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }
    }
}
