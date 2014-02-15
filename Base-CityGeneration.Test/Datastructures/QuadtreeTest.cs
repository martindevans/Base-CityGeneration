using System;
using System.Linq;
using Base_CityGeneration.Datastructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Test.Datastructures
{
    [TestClass]
    public class QuadtreeTest
    {
        [TestMethod]
        public void CreateQuadtree()
        {
            var q = new Quadtree(new RectangleF(0, 0, 100, 100));

            Assert.IsNotNull(q.Root);
            Assert.AreEqual(new RectangleF(0, 0, 100, 100), q.Root.Bounds);
        }

        [TestMethod]
        public void SplitQuadtree()
        {
            var q = new Quadtree(new RectangleF(0, 0, 100, 100));

            q.Root.Split();

            Assert.AreEqual(new RectangleF(0, 0, 50, 50), q.Root.Children.ToArray()[0].Bounds);
            Assert.AreEqual(new RectangleF(50, 0, 50, 50), q.Root.Children.ToArray()[1].Bounds);
            Assert.AreEqual(new RectangleF(0, 50, 50, 50), q.Root.Children.ToArray()[2].Bounds);
            Assert.AreEqual(new RectangleF(50, 50, 50, 50), q.Root.Children.ToArray()[3].Bounds);
        }

        [TestMethod]
        public void QueryQuadtreeByPoint()
        {
            var q = new Quadtree(new RectangleF(0, 0, 100, 100));

            q.Root.Split();
            q.Root.Children.First().Split();

            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var n = q.ContainingNode(new Vector2(r.Next(1, 100), r.Next(1, 100)));
                Assert.IsNotNull(n);
            }
        }

        [TestMethod]
        public void QueryQuadtreeByRectangle()
        {
            var q = new Quadtree(new RectangleF(-50, -50, 100, 100));
            q.Root.Split();
            q.Root.Children.First().Split();

            var r = new RectangleF(-10, -10, 10, 10);

            var result = q.IntersectingLeaves(r);
            Assert.AreEqual(4, result.Count());
        }

        [TestMethod]
        public void QueryQuadtreeByQuadrangleConvex()
        {
            var q = new Quadtree(new RectangleF(-50, -50, 100, 100));
            q.Root.Split();
            q.Root.Children.First().Split();

            var quad = new Vector2[]
            {
                new Vector2(-15, -15),
                new Vector2(0, -10),
                new Vector2(0, 0),
                new Vector2(-10, 0)
            };

            var result = q.IntersectingLeaves(quad);
            Assert.AreEqual(4, result.Count());
        }

        [TestMethod]
        public void GetQuadtreeRootNodeSibling()
        {
            Quadtree q = new Quadtree(new RectangleF(0, 0, 10, 10));

            Assert.IsNull(q.Root.Sibling(Quadtree.Node.Sides.Right));
            Assert.IsNull(q.Root.Sibling(Quadtree.Node.Sides.Left));
            Assert.IsNull(q.Root.Sibling(Quadtree.Node.Sides.Up));
            Assert.IsNull(q.Root.Sibling(Quadtree.Node.Sides.Down));
        }

        [TestMethod]
        public void GetQuadtreeNodeSiblingWithinSameParent()
        {
            Quadtree q = new Quadtree(new RectangleF(0, 0, 10, 10));
            q.Root.Split();

            Assert.AreEqual(q.Root.TopRight, q.Root.TopLeft.Sibling(Quadtree.Node.Sides.Right));
            Assert.IsNull(q.Root.TopLeft.Sibling(Quadtree.Node.Sides.Left));
        }

        [TestMethod]
        public void GetQuadtreeNodeSiblingWithinDifferentParent()
        {
            Quadtree q = new Quadtree(new RectangleF(0, 0, 10, 10));
            q.Root.Split();
            foreach (var child in q.Root.Children)
                child.Split();

            var result = q.Root.TopLeft.TopRight.Sibling(Quadtree.Node.Sides.Right);
            Assert.AreEqual(q.Root.TopRight.TopLeft, result);
        }
    }
}
