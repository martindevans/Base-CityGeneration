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
        private readonly NeighbourSet<int> _set = new NeighbourSet<int>();

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
                ), 1);

                Assert.AreEqual(i + 1, _set.Count);
            }
        }

        [TestMethod]
        public void AssertThat_OverlappingLineSegments_AreNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 1);
            _set.Add(new LineSegment2D(new Vector2(5, 0), new Vector2(15, 0)), 2);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(1, neighbours.Length);
            Assert.AreEqual(new LineSegment2D(new Vector2(5, 0), new Vector2(15, 0)), neighbours.Single().Segment);
            Assert.AreEqual(0f, neighbours.Single().SegmentOverlapStart);
            Assert.AreEqual(0.5f, neighbours.Single().SegmentOverlapEnd);
            Assert.AreEqual(0.5f, neighbours.Single().QueryOverlapStart);
            Assert.AreEqual(1.0f, neighbours.Single().QueryOverlapEnd);
            Assert.AreEqual(2, neighbours.Single().Value);
        }

        [TestMethod]
        public void AssertThat_OverlappingLineSegments_AreNeighbours_WhenSegmentIsReversed()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 3);
            _set.Add(new LineSegment2D(new Vector2(15, 0), new Vector2(5, 0)), 4);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(1, neighbours.Length);
            Assert.AreEqual(new LineSegment2D(new Vector2(15, 0), new Vector2(5, 0)), neighbours.Single().Segment);
            Assert.AreEqual(0.5f, neighbours.Single().SegmentOverlapStart);
            Assert.AreEqual(1.0f, neighbours.Single().SegmentOverlapEnd);
            Assert.AreEqual(1.0f, neighbours.Single().QueryOverlapStart);
            Assert.AreEqual(0.5f, neighbours.Single().QueryOverlapEnd);
            Assert.AreEqual(4, neighbours.Single().Value);
        }

        [TestMethod]
        public void AssertThat_ParallelButNotColinnearSegments_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 5);
            _set.Add(new LineSegment2D(new Vector2(0, 10), new Vector2(10, 10)), 6);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 1).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }

        [TestMethod]
        public void AssertThat_ParallelButNotColinnearSegments_Reversed_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 5);
            _set.Add(new LineSegment2D(new Vector2(0, -10), new Vector2(10, -10)), 6);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 1).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }

        [TestMethod]
        public void AssertThat_ParallelButNearlyColinnearSegments_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 7);
            _set.Add(new LineSegment2D(new Vector2(5, 0), new Vector2(15, 1f)), 8);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0.9f).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }

        [TestMethod]
        public void AssertThat_ParallelButNotOverlappingSegments_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 9);
            _set.Add(new LineSegment2D(new Vector2(15, 0), new Vector2(25, 0f)), 10);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0.1f).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }

        [TestMethod]
        public void AssertThat_ParallelButNotOverlappingSegments2_AreNotNeighbours()
        {
            _set.Add(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 11);
            _set.Add(new LineSegment2D(new Vector2(-15, 0), new Vector2(-25, 0f)), 12);

            var neighbours = _set.Neighbours(new LineSegment2D(new Vector2(0, 0), new Vector2(10, 0)), 0, 0.1f).ToArray();

            // ReSharper disable once ExceptionNotDocumented
            Assert.AreEqual(0, neighbours.Length);
        }
    }
}
