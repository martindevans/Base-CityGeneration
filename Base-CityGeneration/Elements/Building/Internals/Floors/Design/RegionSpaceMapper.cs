using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Myre.Collections;
using SquarifiedTreemap.Model;
using SquarifiedTreemap.Model.Input;
using SquarifiedTreemap.Model.Output;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    internal class RegionSpaceMapper
    {
        private readonly BoundingRectangle _bounds;

        public RegionSpaceMapper(BoundingRectangle bounds)
        {
            _bounds = bounds;
        }

        public IEnumerable<KeyValuePair<BoundingRectangle, BaseSpaceSpec>> Map(IEnumerable<RoomTreemapNode> spaces)
        {
            //The layout of the rooms in the tree map depends entirely upon the shape of the tree!
            //Simply through experimentation I have found balanced trees to be the best.
            var root = new Tree<RoomTreemapNode>.Node();
            var orderedSpaces = spaces.OrderByDescending(a => a.Area);
            BuildBalancedBinaryTree(orderedSpaces, root);

            //Build a treemap to assign spaces to the nodes of the tree
            var treemap = Treemap<RoomTreemapNode>.Build(_bounds, new Tree<RoomTreemapNode>(root));

            //Rearrange the treemap to satisfy constraints (where possible)
            ImproveConstraintSatisfaction(treemap);

            //Rearrange the treemap to satisfy connections (where possible, without disrupting any constraints)
            ImproveConnectionSatisfaction(treemap);

            return from space in WalkTree(treemap.Root)
                   select new KeyValuePair<BoundingRectangle, BaseSpaceSpec>(space.Bounds, space.Value.Space);
        }

        

        #region improve constraints
        /// <summary>
        /// We can freely swap parts within the treemap around in an attempt to satisfy more constraints
        /// </summary>
        /// <param name="treemap"></param>
        private static void ImproveConstraintSatisfaction(Treemap<RoomTreemapNode> treemap)
        {
            //Measure the initial constraint satisfaction, we will iteratively improve this bound until we cannot improve any more
            var sat = MeasureConstraintSat(treemap);

            for (var i = 0; i < 128; i++)
            {
                
            }

            //throw new NotImplementedException();
        }

        /// <summary>
        /// We can freely swap parts within the treemap around in an attempt to bring rooms next to one another
        /// </summary>
        /// <param name="treemap"></param>
        private static void ImproveConnectionSatisfaction(Treemap<RoomTreemapNode> treemap)
        {
            //throw new NotImplementedException();
        }

        private static float MeasureConstraintSat(Treemap<RoomTreemapNode> treemap)
        {
            return 0;
        }
        #endregion

        #region tree building
        private static void SanityCheckPreBuildTree(Tree<RoomTreemapNode>.Node root)
        {
            if (root.Count != 0)
                throw new InvalidOperationException("Cannot build tree when root is not empty");
        }

        /// <summary>
        /// Simply add all the nodes to the root node
        /// </summary>
        /// <param name="spaces"></param>
        /// <param name="root"></param>
        /// <remarks>This results in *all* rooms being split the same direction - quite a terrible layout</remarks>
        private static void BuildFlatTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root)
        {
            SanityCheckPreBuildTree(root);

            foreach (var roomTreemapNode in spaces)
                root.Add(new Tree<RoomTreemapNode>.Node(roomTreemapNode));
        }

        /// <summary>
        /// Build an unbalanced right recursive tree - each node contains a single room and a link to the next node (on the right)
        /// </summary>
        /// <param name="spaces"></param>
        /// <param name="root"></param>
        /// <remarks>Generates acceptable layouts for smallest rooms, large rooms tend to have fairly bad aspect ratios</remarks>
        private static void BuildRightRecursiveTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root)
        {
            SanityCheckPreBuildTree(root);

            var addTo = root;
            foreach (var roomTreemapNode in spaces)
            {
                var a = new Tree<RoomTreemapNode>.Node();
                addTo.Add(a);
                addTo = a;
                addTo.Add(new Tree<RoomTreemapNode>.Node(roomTreemapNode));
            }
        }

        /// <summary>
        /// Build an unbalanced left recursive tree - each node contains a single room and a link to the next node (on the left)
        /// </summary>
        /// <param name="spaces"></param>
        /// <param name="root"></param>
        /// <remarks>Mirror images of right recursive (obviously...)</remarks>
        private static void BuildLeftRecursiveTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root)
        {
            SanityCheckPreBuildTree(root);

            var addTo = root;
            foreach (var roomTreemapNode in spaces)
            {
                var a = new Tree<RoomTreemapNode>.Node();
                addTo.Add(a);
                addTo.Add(new Tree<RoomTreemapNode>.Node(roomTreemapNode));
                addTo = a;
            }
        }

        /// <summary>
        /// Build a balanced binary tree
        /// </summary>
        /// <param name="spaces"></param>
        /// <param name="root"></param>
        private static void BuildBalancedBinaryTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root)
        {
            SanityCheckPreBuildTree(root);
            ExtendBalancedBinaryTree(new ArraySegment<RoomTreemapNode>(spaces.ToArray()), root, 0);
        }

        private static void ExtendBalancedBinaryTree(ArraySegment<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root, int depth)
        {
            //We can't build a *perfectly* balanced binary tree with a potentially odd number of nodes. How we distribute those odd nodes is important

            //Going all the way down to 2 nodes doesn't give us quite enough material to work with to split spaces, so we flatten with child nodes of 3
            if (spaces.Count <= 3)
            {
                if (depth % 2 == 0)
                    BuildLeftRecursiveTree(spaces, root);
                else
                    BuildRightRecursiveTree(spaces, root);
            }
            else
            {
                var lCount = (int)Math.Ceiling(spaces.Count / 2f);
                var leftSpaces = new ArraySegment<RoomTreemapNode>(spaces.Array, spaces.Offset, lCount);
                var left = new Tree<RoomTreemapNode>.Node();
                root.Add(left);

                var rightSpaces = new ArraySegment<RoomTreemapNode>(spaces.Array, spaces.Offset + lCount, spaces.Count - lCount);
                var right = new Tree<RoomTreemapNode>.Node();
                root.Add(right);

                //If the two sides are uneven, which side should be put the additional space?
                if (rightSpaces.Count != leftSpaces.Count)
                {
                    //Put the space on the side which *maximises* the variance
                    var leftVariance = leftSpaces.Take(lCount - 1).Max(a => a.Area) / leftSpaces.Take(lCount - 1).Min(a => a.Area);
                    var rightVariance = rightSpaces.Take(lCount - 1).Max(a => a.Area) / rightSpaces.Take(lCount - 1).Min(a => a.Area);

                    if (rightVariance > leftVariance)
                    {
                        //Left already contains the additional space, so we need to swap them over (extend right, shrink left)
                        leftSpaces = new ArraySegment<RoomTreemapNode>(leftSpaces.Array, leftSpaces.Offset, leftSpaces.Count - 1);
                        rightSpaces = new ArraySegment<RoomTreemapNode>(rightSpaces.Array, rightSpaces.Offset - 1, rightSpaces.Count + 1);
                    }
                }

                ExtendBalancedBinaryTree(leftSpaces, left, depth + 1);
                ExtendBalancedBinaryTree(rightSpaces, right, depth + 1);
            }
        }

        /// <summary>
        /// Build a tree by creating a new node every time estimated aspect ratio climbs above a certain threshold
        /// </summary>
        /// <param name="spaces"></param>
        /// <param name="root"></param>
        /// <param name="bounds"></param>
        /// <remarks>This totally sucks.</remarks>
        private static void BuildAdaptiveTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root, BoundingRectangle bounds)
        {
            SanityCheckPreBuildTree(root);

            //Each node can either stack up next to the previous, or switch direction and occupy some of the remaining space
            //Add every node to a common parent, if we change which way around is best, add a new node to the tree and add all future nodes to that
            bool? currentSplitVertical = null;
            var addTo = root;
            var remainingSpace = bounds;
            foreach (var node in spaces)
            {
                //measure aspect ratio for vertical and horizontal fit into remaining space
                var szVert = remainingSpace.Extent.Y / node.Area;
                var arVert = remainingSpace.Extent.Y * szVert;
                var szHorz = remainingSpace.Extent.X / node.Area;
                var arHorz = remainingSpace.Extent.X * szHorz;

                //Choose the best split direction
                var splitVert = arVert < arHorz;

                //If it's changed create a new node and attach the old one to the parent
                if (!currentSplitVertical.HasValue || splitVert != currentSplitVertical.Value)
                {
                    var inner = new Tree<RoomTreemapNode>.Node();
                    addTo.Add(inner);
                    addTo = inner;

                    currentSplitVertical = splitVert;
                }

                addTo.Add(new Tree<RoomTreemapNode>.Node(node));

                remainingSpace = new BoundingRectangle(remainingSpace.Min, remainingSpace.Max - (splitVert ? new Vector2(szVert, 0) : new Vector2(0, szHorz)));
            }
        }
        #endregion

        private static IEnumerable<Node<T>> WalkTree<T>(Node<T> root) where T : ITreemapNode
        {
            if (root.Value != null)
                yield return root;

            foreach (var node in root)
                foreach (var child in WalkTree(node))
                    yield return child;
        }
    }

    internal class RoomTreemapNode
        : ITreemapNode
    {
        public BaseSpaceSpec Space { get; private set; }

        public float Area { get; set; }

        public float MinArea { get; private set; }
        public float MaxArea { get; private set; }

        public RoomTreemapNode(BaseSpaceSpec assignedSpace, Func<double> random, INamedDataCollection metadata)
        {
            Space = assignedSpace;

            MinArea = assignedSpace.MinArea(random, metadata);
            MaxArea = assignedSpace.MaxArea(random, metadata);

            Area = MinArea;
        }

        float? ITreemapNode.Area
        {
            get { return Area; }
        }
    }
}
