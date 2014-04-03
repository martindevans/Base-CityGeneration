using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge;
using Microsoft.Xna.Framework;
using Myre.Extensions;

namespace Base_CityGeneration.Datastructures.Extensions
{
    public static class QuadtreeExtensions
    {
        public static Mesh ToHalfEdgeMesh(this Quadtree quadtree)
        {
            Mesh m = new Mesh();

            Tessellate(m, quadtree.Root);

            return m;
        }

        private static void Tessellate(Mesh m, Quadtree.Node node)
        {
            if (node.IsLeaf)
            {
                var l = SiblingSide(node, Quadtree.Node.Sides.Left);
                var u = SiblingSide(node, Quadtree.Node.Sides.Up);
                var r = SiblingSide(node, Quadtree.Node.Sides.Right);
                var d = SiblingSide(node, Quadtree.Node.Sides.Down);

                var points =
                    l.DropLast(1)
                    .Append(u.DropLast(1))
                    .Append(r.DropLast(1))
                    .Append(d.DropLast(1));

                m.GetOrConstructFace(points.Select(m.GetOrConstructVertex).ToArray());
            }
            else
                foreach (var c in node.Children)
                    Tessellate(m, c);
        }

        #region sibling
        public static bool IsOnSide(this Quadtree.Node.Positions position, Quadtree.Node.Sides side)
        {
            if (position == Quadtree.Node.Positions.Root)
                throw new ArgumentException("position");

            return (((int) position) & ((int) side)) != 0;
        }

        public static Quadtree.Node.Sides Opposite(this Quadtree.Node.Sides side)
        {
            switch (side)
            {
                case Quadtree.Node.Sides.Up:
                    return Quadtree.Node.Sides.Down;
                case Quadtree.Node.Sides.Down:
                    return Quadtree.Node.Sides.Up;
                case Quadtree.Node.Sides.Right:
                    return Quadtree.Node.Sides.Left;
                case Quadtree.Node.Sides.Left:
                    return Quadtree.Node.Sides.Right;
                default:
                    throw new ArgumentOutOfRangeException("side");
            }
        }

        private static IEnumerable<Vector2> SiblingSide(Quadtree.Node node, Quadtree.Node.Sides siblingSide)
        {
            var sibling = node.Sibling(siblingSide);
            if (sibling != null)
            {
                //Enumerate opposite side of sibling (i.e. the border with this node)
                return Side(sibling, siblingSide.Opposite()).Reverse();
            }
            else
            {
                //Enumerate the side of this node instead, since there is no sibling in that direction
                return Side(node, siblingSide);
            }
        }

        #endregion

        #region side enumeration
        private static IEnumerable<Vector2> Side(Quadtree.Node node, Func<Rectangle, IEnumerable<Vector2>> pointsAlongSide, Func<Quadtree.Node, IEnumerable<Quadtree.Node>> nodesAlongSide)
        {
            if (node.IsLeaf)
            {
                foreach (var point in pointsAlongSide(node.Bounds))
                    yield return point;
            }
            else
            {
                bool first = true;
                foreach (var n in nodesAlongSide(node))
                {
                    var points = Side(n, pointsAlongSide, nodesAlongSide).Skip(first ? 0 : 1);
                    foreach (var point in points)
                        yield return point;
                    first = false;
                }
            }
        }

        private static IEnumerable<Vector2> Side(Quadtree.Node node, Quadtree.Node.Sides side)
        {
            switch (side)
            {
                case Quadtree.Node.Sides.Up:
                    return Side(node, r => new[] { new Vector2(r.Left, r.Bottom), new Vector2(r.Right, r.Bottom) }, n => new[] { n.TopLeft, n.TopRight });
                case Quadtree.Node.Sides.Down:
                    return Side(node, r => new[] { new Vector2(r.Right, r.Top), new Vector2(r.Left, r.Top) }, n => new[] { n.BottomRight, n.BottomLeft });
                case Quadtree.Node.Sides.Left:
                    return Side(node, r => new[] { new Vector2(r.Left, r.Top), new Vector2(r.Left, r.Bottom) }, n => new[] { n.BottomLeft, n.TopLeft });
                case Quadtree.Node.Sides.Right:
                    return Side(node, r => new[] { new Vector2(r.Right, r.Bottom), new Vector2(r.Right, r.Top) }, n => new[] { n.TopRight, n.BottomRight });
                default:
                    throw new ArgumentOutOfRangeException("side");
            }
        }
        #endregion
    }
}