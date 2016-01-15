using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
using Myre.Collections;
using Myre.Extensions;
using SquarifiedTreemap.Extensions;
using SquarifiedTreemap.Model;
using SquarifiedTreemap.Model.Input;
using SquarifiedTreemap.Model.Output;
using SwizzleMyVectors.Geometry;

using HeMesh = Base_CityGeneration.Datastructures.HalfEdge.Mesh<
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceCornerVertex,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceWall,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceFace
>;
using HeVertex = Base_CityGeneration.Datastructures.HalfEdge.Vertex<
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceCornerVertex,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceWall,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceFace
>;
using HeFace = Base_CityGeneration.Datastructures.HalfEdge.Face<
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceCornerVertex,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceWall,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceFace
>;
using HeHalfEdge = Base_CityGeneration.Datastructures.HalfEdge.HalfEdge<
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceCornerVertex,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceWall,
    Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping.SpaceFace
>;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping
{
    internal class Treemapper
        : ISpaceMapper
    {
        public Mesh<SpaceCornerVertex, SpaceWall, SpaceFace> Map(FloorplanRegion region, IEnumerable<KeyValuePair<BaseSpaceSpec, float>> spaces, Func<double> random, INamedDataCollection metadata)
        {
            var treemap = Map(region, spaces.Select(a => new RoomTreemapNode(a.Key, a.Value)));

            return ExtractMesh(region, treemap);
        }

        #region mesh generation from treemap
        private Mesh<SpaceCornerVertex, SpaceWall, SpaceFace> ExtractMesh(FloorplanRegion region, Treemap<RoomTreemapNode> treemap)
        {
            var mesh = new Mesh<SpaceCornerVertex, SpaceWall, SpaceFace>();

            //Create root face. This is the OABR which this means all cuts will be on convex shapes and slicing is a lot simpler
            var root = mesh.GetOrConstructFace(treemap.StartSpace.GetCorners().Select(a => mesh.GetOrConstructVertex(a)).ToArray());

            //Recursively descend down the tree, cutting each face as the treemap specifies
            SubdivideFace(treemap.Root, mesh, root);

            //Transform the vertices from OABB space into world space
            mesh.Transform(region.OABR.ToWorld);

            return mesh;
        }

        private void SubdivideFace(Node<RoomTreemapNode> node, HeMesh mesh, Face<SpaceCornerVertex, SpaceWall, SpaceFace> face)
        {
            //Find all children which are not zero size.
            //Sometimes we generate zero size nodes due to the way the tree is built up so we need to ignore them
            var children = node.Where(a => a.Length > 0).ToArray();

            if (children.Length == 0)
            {
                //No children! Assign the parent (this) node instead
                face.Tag = new SpaceFace(node.Value.Space);
            }
            else if (children.Length == 1)
            {
                //Only 1 child, just recurse into it with the full face
                SubdivideFace(children.Single(), mesh, face);
            }
            else
            {
                var min = face.Vertices.Select(a => a.Position).Aggregate(Vector2.Min);

                //More than one child. Work through them splitting the parent face part by part
                float total = 0;
                for (var i = 0; i < children.Length - 1; i++)
                {
                    var child = children[i];

                    //Determine the split line
                    Ray2 splitRay = node.SplitVertical
                        ? new Ray2(new Vector2(total + child.Length + min.X, min.Y - 10), new Vector2(0, 1))
                        : new Ray2(new Vector2(min.X - 10, total + child.Length + min.Y), new Vector2(1, 0));
                    total += child.Length;

                    //Find the two edges which intersect this line
                    var intersectingEdges = (from edge in face.Edges
                                            let seg = new LineSegment2(edge.Pair.EndVertex.Position, edge.EndVertex.Position)
                                            let intersection = seg.Intersects(splitRay)
                                            where intersection != null
                                            select new { edge, vertex = mesh.GetOrConstructVertex(intersection.Value.Position) }).ToArray();

                    //Sanity check: The shape is convex, and split by a line. It should be intersected in exactly 2 places.
                    if (intersectingEdges.Length != 2)
                        throw new InvalidOperationException(string.Format("Expected split line to intersect 2 edges, found {0}", intersectingEdges.Length));

                    //Split both the edges with their associated vertex
                    foreach (var intersectingEdge in intersectingEdges)
                    {
                        HeHalfEdge _, __;
                        mesh.Split(intersectingEdge.edge, intersectingEdge.vertex, out _, out __);
                    }

                    //Split the face with the two vertices
                    HeFace f1, f2;
                    mesh.Split(face, intersectingEdges[0].vertex, intersectingEdges[1].vertex, out f1, out f2);

                    //Recursively subdivide this face
                    SubdivideFace(child, mesh, f1);

                    //If this is the second to last child then the remainder is exactly the right space for the remaining node. In which case recursively subdivide that too
                    //otherwise set face to be the remainder and move onto the next child node
                    if (i == children.Length - 2)
                        SubdivideFace(children[i + 1], mesh, f2);
                    else
                        face = f2;
                }
            }
        }
        #endregion

        private static Treemap<RoomTreemapNode> Map(FloorplanRegion region, IEnumerable<RoomTreemapNode> spaces)
        {
            Contract.Requires(spaces != null);
            Contract.Ensures(Contract.Result<Treemap<RoomTreemapNode>>() != null);

            //The layout of the rooms in the tree map depends entirely upon the shape of the tree!
            //Simply through experimentation I have found balanced trees to be the best.
            var root = new Tree<RoomTreemapNode>.Node();
            BuildTree(spaces, root);

            //Build a treemap to assign spaces to the nodes of the tree
            var treemap = Treemap<RoomTreemapNode>.Build(new BoundingRectangle(region.OABR.Min, region.OABR.Max), new Tree<RoomTreemapNode>(root));

            //Rearrange the treemap to satisfy constraints (where possible)
            ImproveConstraintSatisfaction(region, treemap);

            return treemap;
        }

        #region improve constraints
        /// <summary>
        /// We can freely swap parts within the treemap around in an attempt to satisfy more constraints
        /// </summary>
        /// <param name="region"></param>
        /// <param name="treemap"></param>
        private static void ImproveConstraintSatisfaction(FloorplanRegion region, Treemap<RoomTreemapNode> treemap)
        {
            Contract.Requires(region != null);
            Contract.Requires(treemap != null);

            var values = WalkTreeValues(treemap.Root).ToArray();

            //Assign a score to every node. Leaf nodes score = satisfaction of this room. Inner nodes = function of leaf node scores.
            WalkTreeValues(treemap.Root).ForEach(a => a.Value.ConstraintSatisfaction = MeasureLeafUnSat(region, a));
            var unsatMap = WalkTreeValues(treemap.Root).ToDictionary(a => a, a => a.Value.ConstraintSatisfaction);

            CalculateInnerUnSat(treemap.Root, unsatMap);

            //todo: [floorplan] rearrange nodes in tree to improve satisfaction!
        }

        /// <summary>
        /// unsat of inner nodes is a function of the unsat of the child nodes
        /// </summary>
        /// <param name="root"></param>
        /// <param name="unsatMap"></param>
        private static void CalculateInnerUnSat(Node<RoomTreemapNode> root, IDictionary<Node<RoomTreemapNode>, float> unsatMap)
        {
            foreach (var node in root.WalkTreeBottomUp().Where(a => !unsatMap.ContainsKey(a)))
            {
                unsatMap[node] = node.Select(a => {
                    float val;
                    return unsatMap.TryGetValue(a, out val) ? val : 0;
                }).Aggregate(1.0f, (a, b) => Math.Max(1, a) * Math.Max(1, b));
            }
        }

        private static float MeasureLeafUnSat(FloorplanRegion region, Node<RoomTreemapNode> leaf)
        {
            Contract.Requires(region != null);
            Contract.Requires(leaf != null && leaf.Value != null);

            //Calculate the intersection between the floor plan and the rectangle assigned to this room
            var intersection = leaf.Bounds.GetCorners().Intersection2D(region.Points).Select(a => a.ToArray()).ToArray();

            //If the intersection generates no parts we have a problem! This rooms is unsat to the order of it's area (since it has no area)
            if (!intersection.Any())
                return leaf.Value.Area;

            //If we have 1 intersection we're good to go, otherwise take the largest
            Vector2[] intersectionShape;
            if (intersection.Length == 1)
                intersectionShape = intersection[0];
            else
                intersectionShape = intersection.Select(a => new { Area = a.Area(), Shape = a }).Aggregate((a, b) => a.Area > b.Area ? a : b).Shape;

            //Cut out a subregion using this shape, this gets us useful information about walls (adjacency, windows, doors etc)
            var subregion = region.SubRegion(intersectionShape);

            return leaf.Value.Space.Constraints.Select(c =>
            {

                region.SubRegion(leaf.Bounds.GetCorners());

                //Measure how unsatisfed this constraint is in this position
                if (c.Requirement.IsSatisfied(subregion))
                    return 0;

                return 1;
            }).Aggregate(1.0f, (a, b) => Math.Max(1, a) * Math.Max(1, b));
        }
        #endregion

        #region tree building
        private static void BuildTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root)
        {
            //Build whichever tree we want (balanced bianry seems to get best results, may we can do something cleverer in the future)
            var orderedSpaces = spaces.OrderByDescending(a => a.Area);
            BuildBalancedBinaryTree(orderedSpaces, root);

            //Clean up the tree (remove branches of entirely null nodes)
            RecursiveRemoveNullBranch(root);
        }

        /// <summary>
        /// Delete this node if all the child nodes are deleted and this node has a null value
        /// </summary>
        /// <param name="root"></param>
        private static bool RecursiveRemoveNullBranch(INode<RoomTreemapNode> root)
        {
            var children = root.ToArray();
            foreach (var node in children)
            {
                if (RecursiveRemoveNullBranch(node))
                    root.Remove(node);
            }

            return root.Count == 0 && root.Value == null;
        }

        [ContractAbbreviator]
        private static void SanityCheckBuildTree(Tree<RoomTreemapNode>.Node root)
        {
            Contract.Requires(root != null && root.Count == 0);
        }

        /// <summary>
        /// Simply add all the nodes to the root node
        /// </summary>
        /// <param name="spaces"></param>
        /// <param name="root"></param>
        /// <remarks>This results in *all* rooms being split the same direction - quite a terrible layout</remarks>
        private static void BuildFlatTree(IEnumerable<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root)
        {
            Contract.Requires(spaces != null);
            SanityCheckBuildTree(root);

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
            Contract.Requires(spaces != null);
            SanityCheckBuildTree(root);

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
            Contract.Requires(spaces != null);
            SanityCheckBuildTree(root);

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
            Contract.Requires(spaces != null);
            SanityCheckBuildTree(root);

            ExtendBalancedBinaryTree(new ArraySegment<RoomTreemapNode>(spaces.ToArray()), root, 0);
        }

        private static void ExtendBalancedBinaryTree(ArraySegment<RoomTreemapNode> spaces, Tree<RoomTreemapNode>.Node root, int depth)
        {
            SanityCheckBuildTree(root);

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

                Contract.Assume(leftSpaces.Count > 0);
                Contract.Assume(rightSpaces.Count > 0);
                Contract.Assume(rightSpaces.Offset == leftSpaces.Offset + leftSpaces.Count);

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
            Contract.Requires(spaces != null);
            SanityCheckBuildTree(root);

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

        #region static helpers
        /// <summary>
        /// Walk all nodes in the tree which have a value associated with them
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <returns></returns>
        private static IEnumerable<Node<T>> WalkTreeValues<T>(Node<T> root) where T : ITreemapNode
        {
            return root.WalkTreeBottomUp().Where(a => a.Value != null);
        }
        #endregion
    }

    
}
