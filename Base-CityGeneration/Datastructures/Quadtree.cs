using System;
using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Datastructures.HalfEdge.Extensions;
using Microsoft.Xna.Framework;

namespace Base_CityGeneration.Datastructures
{
    public class Quadtree
    {
        public Node Root { get; private set; }

        public Quadtree(RectangleF bounds)
        {
            Root = new Node(bounds, null, Node.Positions.Root);
        }

        public Node ContainingNode(Vector2 point)
        {
            if (!Root.Contains(point))
                return null;

            var node = Root;
            while (node != null && !node.IsLeaf)
                node = node.Children.SingleOrDefault(n => n.Contains(point));

            return node;
        }

        public IEnumerable<Node> IntersectingLeaves(RectangleF r)
        {
            return Root.IntersectingLeaves(r);
        }

        public IEnumerable<Node> IntersectingLeaves(Vector2[] convex)
        {
            return Root.IntersectingLeaves(convex);
        }

        public IEnumerable<Node> IntersectingLeaves(Path p)
        {
            if (p == null)
                return new Node[0];

            return p.Quadrangles.SelectMany(q => Root.IntersectingLeaves(q)).Distinct();
        }

        public class Node
        {
            public enum Positions
            {
                TopLeft = Sides.Up | Sides.Left,
                TopRight = Sides.Up | Sides.Right,
                BottomLeft = Sides.Down | Sides.Left,
                BottomRight = Sides.Down | Sides.Right,
                Root = 0
            }

            public enum Sides
            {
                Up = 1,
                Down = 2,
                Right = 4,
                Left = 8
            }

            public RectangleF Bounds { get; private set; }

            public Node TopLeft { get; private set; }
            public Node TopRight { get; private set; }
            public Node BottomLeft { get; private set; }
            public Node BottomRight { get; private set; }

            public Node this[Positions position]
            {
                get
                {
                    switch (position)
                    {
                        case Positions.TopLeft:
                            return TopLeft;
                        case Positions.TopRight:
                            return TopRight;
                        case Positions.BottomLeft:
                            return BottomLeft;
                        case Positions.BottomRight:
                            return BottomRight;
                        case Positions.Root:
                            if (Parent != null)
                                return Parent[Positions.Root];
                            else
                                return this;
                        default:
                            throw new ArgumentOutOfRangeException("position");
                    }
                }
            }

            public int Depth { get; private set; }

            public IEnumerable<Node> Children
            {
                get
                {
                    if (IsLeaf)
                        yield break;
                    if (TopLeft != null)
                        yield return TopLeft;
                    if (TopRight != null)
                        yield return TopRight;
                    if (BottomLeft != null)
                        yield return BottomLeft;
                    if (BottomRight != null)
                        yield return BottomRight;
                }
            }

            public IEnumerable<Node> Leaves
            {
                get
                {
                    if (IsLeaf)
                        yield return this;
                    else
                        foreach (var leaf in Children.SelectMany(c => c.Leaves))
                            yield return leaf;
                }
            }

            public bool IsLeaf { get; private set; }
            public Node Parent { get; private set; }
            public Positions Position { get; private set; }

            public Node(RectangleF bounds, Node parent, Positions position)
            {
                Bounds = bounds;
                IsLeaf = true;
                Parent = parent;
                Depth = parent == null ? 0 : parent.Depth + 1;
                Position = position;
            }

            public void Split()
            {
                if (!IsLeaf)
                    throw new InvalidOperationException("Cannot split a non leaf node");
                IsLeaf = false;

                var w = Bounds.Width / 2;
                var h = Bounds.Height / 2;

                var midX = Bounds.Left + w;
                var midY = Bounds.Top + h;

                TopLeft = new Node(new RectangleF(Bounds.Left, Bounds.Top, w, h), this, Positions.TopLeft);
                TopRight = new Node(new RectangleF(midX, Bounds.Top, w, h), this, Positions.TopRight);
                BottomLeft = new Node(new RectangleF(Bounds.Left, midY, w, h), this, Positions.BottomLeft);
                BottomRight = new Node(new RectangleF(midX, midY, w, h), this, Positions.BottomRight);
            }

            public bool Contains(Vector2 point)
            {
                return Bounds.Contains(point);
            }

            public bool Intersects(RectangleF rect)
            {
                return Bounds.Intersects(rect);
            }

            public bool Intersects(Vector2[] convex)
            {
                return SeparatingAxisTester.Intersects(convex, Bounds.ToConvex());
            }

            public IEnumerable<Node> IntersectingLeaves(RectangleF r)
            {
                return RecursiveLeafSelector(n => n.Intersects(r));
            }

            public IEnumerable<Node> IntersectingLeaves(Vector2[] convex)
            {
                return RecursiveLeafSelector(n => n.Intersects(convex));
            }

            private IEnumerable<Node> RecursiveLeafSelector(Func<Node, bool> predicate)
            {
                bool acceptable = predicate(this);
                if (!acceptable)
                    yield break;

                if (IsLeaf)
                    yield return this;
                else
                    foreach (var n in Children.SelectMany(c => c.RecursiveLeafSelector(predicate)))
                        yield return n;
            }

            /// <summary>
            /// Get the sibling (node at same depth) in a given direction
            /// </summary>
            /// <param name="side"></param>
            /// <returns>The sibling, or null if the tree does not extend to the necessary depth</returns>
            public Node Sibling(Sides side)
            {
                if (Parent == null)
                    return null;

                if (Position.IsOnSide(side))
                {
                    //Sibling is in another parent node

                    if (Parent.Parent == null)
                        return null;    //There is no other parent node!

                    var p = Position;
                    var opposite = side.Opposite();

                    var p2 = (Positions)(((int)p ^ (int)side) | (int)opposite);

                    var parentSibling = Parent.Sibling(side);
                    if (parentSibling == null)
                        return null;

                    return parentSibling[p2];
                }
                else
                {
                    //Sibling is the opposite node within the same parent
                    var opposite = side.Opposite();
                    var p = Position;

                    //Remove opposite direction and add in direction
                    var p2 = (Positions)(((int)p ^ (int)opposite) | (int)side);

                    return Parent[p2];
                }
            }
        }
    }
}
