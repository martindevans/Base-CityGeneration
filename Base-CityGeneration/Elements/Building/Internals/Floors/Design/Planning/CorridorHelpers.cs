using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using ClipperLib;
using EpimetheusPlugins.Extensions;
using Placeholder.AI.Pathfinding.Graph;
using Placeholder.AI.Pathfinding.Graph.NodeGraph;
using Placeholder.AI.Pathfinding.SpanningTree;
using SwizzleMyVectors;
using Face = Base_CityGeneration.Datastructures.HalfEdge.Face<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using HalfEdge = Base_CityGeneration.Datastructures.HalfEdge.HalfEdge<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Vertex = Base_CityGeneration.Datastructures.HalfEdge.Vertex<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Mesh = Base_CityGeneration.Datastructures.HalfEdge.Mesh<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    internal static class CorridorHelpers
    {
        /// <summary>
        /// Given a spanning tree convert it into a geometrically equivalent spanning tree, but simpler
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static Tree<PositionVertex, CostEdge<PositionVertex>> SimplifySpanningTree(Tree<Vertex, HalfEdge> tree)
        {
            Contract.Requires(tree != null);
            Contract.Ensures(Contract.Result<Tree<PositionVertex, CostEdge<PositionVertex>>>() != null);

            //The spanning tree has a load of vertices in it which we don't really need!
            //Convert into a node graph, apply linear simplification, then convert back into a tree

            //Convert into a node graph
            var vertexDictionary = tree.Vertices.ToDictionary(v => v, v => new PositionVertex(To3(v.Position)));
            foreach (var edge in tree.Edges)
            {
                var a = vertexDictionary[edge.StartVertex];
                var b = vertexDictionary[edge.EndVertex];
                a.AddNeighbour(b);
            }

            //Apply linear simplification
            var vertexSet = new HashSet<PositionVertex>(vertexDictionary.Values);
            Func<ISet<PositionVertex>, bool> remove = set =>
            {

                //Find all vertices which we might want to remove
                var removalCandidates = vertexSet.Where(v => v.OutwardEdges.Count() == 2).ToArray();
                bool modified = false;
                for (int i = 0; i < removalCandidates.Length; i++)
                {
                    var vertex = removalCandidates[i];

                    //Get the three vertices involved
                    var a = vertex.OutwardEdges.First().End;
                    var b = vertex;
                    var c = vertex.OutwardEdges.Skip(1).First().End;

                    //Create the line segments which connect these vertices
                    var ab = Vector3.Normalize(b.Position - a.Position);
                    var bc = Vector3.Normalize(c.Position - b.Position);

                    //Check tolerance of 5 degrees (cosine(5 degrees) == 0.996194698)
                    var dot = Vector3.Dot(ab, bc);
                    if (dot >= 0.996194698f)
                    {
                        //Delete the vertex from the set of all vertices. This isn't enough because we need to patch up the neighbours!
                        var removed = vertexSet.Remove(b);
                        Contract.Assume(removed);
                        modified = true;

                        //Remove links to the vertex we just deleted
                        a.RemoveNeighbour(b);
                        c.RemoveNeighbour(b);

                        //Add back links directly between the two neighbours
                        a.AddNeighbour(c);
                        c.AddNeighbour(a);
                    }
                }

                return modified;
            };

            //Keep applying the removal algorithm above until fixpoint
            remove.Fixpoint(vertexSet);

            //Convert back into a tree
            var t = new Tree<PositionVertex, CostEdge<PositionVertex>>(
                vertexSet,
                vertexSet.SelectMany(v => v.OutwardEdges).Where(e => vertexSet.Contains(e.End))
            );

            return t;
        }

        /// <summary>
        /// Remove unwanted nodes from a corridor spanning tree
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="width">width of the corridor</param>
        /// <returns></returns>
        public static Tree<PositionVertex, CostEdge<PositionVertex>> CleanSpanningTree(Tree<PositionVertex, CostEdge<PositionVertex>> tree, float width)
        {
            var vertexSet = new HashSet<PositionVertex>(tree.Vertices);

            Func<ISet<PositionVertex>, bool> remove = (set) => {

                //Build a set of vertices to remove
                var toRemove = tree.Leaves.Where(l => {

                    //Find the neighbour edge of this leaf, if it has *no* neighbours remove it straight away (zero length corridors are no good)
                    var ne = l.OutwardEdges.SingleOrDefault();
                    if (ne == null)
                        return true;
                    var neighbour = ne.End;

                    //If we're *very* close to the neighbour (i.e. within half a corridor width)
                    //and at more than 90degrees turn from any neighbour we'd be inside the corridor!
                    if (Vector3.Distance(l.Position, neighbour.Position) <= width / 2)
                    {
                        //Go through all the neighbour of the neighbour (except self) and measure the turn
                        foreach (var outwardEdge in neighbour.OutwardEdges)
                        {
                            //Don't compare with self, obviously
                            if (outwardEdge.End.Equals(l))
                                continue;

                            //Check angle is >= 90 degrees
                            var ab = neighbour.Position - outwardEdge.End.Position;
                            var bc = l.Position - neighbour.Position;
                            var dot = Vector3.Dot(ab, bc);
                            if (dot <= 0)
                                return true;
                        }
                    }

                    //None of the above cases matched, so we don't want to remove this vertex
                    return false;
                });

                var count = set.Count;
                set.ExceptWith(toRemove);
                return set.Count != count;
            };
            remove.Fixpoint(vertexSet);

            return TreeFromVertices<PositionVertex, CostEdge<PositionVertex>>(vertexSet);
        }

        /// <summary>
        /// Clean up a proposed corridor shape (primarily removing holes, which are invalid in a floorplan)
        /// </summary>
        /// <param name="clipper"></param>
        /// <param name="outline"></param>
        /// <param name="corridor"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static List<List<IntPoint>> CleanCorridorPolygon(Clipper clipper, List<IntPoint> outline, IEnumerable<Vector2> corridor, float width)
        {
            //Clean up after the previous run, just in case
            clipper.Clear();

            //Clip this spanning tree to the footprint of the floorplan (to create a polytree)
            var result = new List<List<IntPoint>>();
            clipper.AddPath(outline, PolyType.ptClip, true);
            clipper.AddPath(corridor.Select(ToPoint).ToList(), PolyType.ptSubject, true);
            clipper.Execute(ClipType.ctIntersection, result);

            //Clean up after self, to be polite
            clipper.Clear();

            //Keep simplifying, and removing holes until nothing happens
            do
            {
                //merge together vertices which are very close
                result = Clipper.CleanPolygons(result, width / 8);

                //Remove holes from the result
                var holes = result.RemoveAll(r => !Clipper.Orientation(r));

                //Once we have one single polygon, or removing holes did nothing we've finished!
                if (result.Count == 1 || holes == 0)
                    return result;

            } while (result.Count > 0);

            //This shouldn't ever happen unless we simplify away to nothing
            return result;
        }

        /// <summary>
        /// Walk a series of paths which cover the entire spanning tree (paths, not polygons!)
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static List<List<IntPoint>> WalkTreePaths(Tree<PositionVertex, CostEdge<PositionVertex>> tree)
        {
            Contract.Requires(tree != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<IReadOnlyList<IntPoint>>>() != null);

            //From each interior node trace out length 2 paths to add connected vertices
            //So if we have a tree like:
            //
            // 1 -> 2 -> 3
            //        -> 4
            //
            //We trace paths from 2 (the only interior node in this example) from all connected nodes to all other connected nodes:
            //
            // 1, 2, 3
            // 1, 2, 4
            // 3, 2, 4

            //First find all the interior nodes
            var interiorNodes = tree.Interior.ToArray();

            //Create a place to store results (with a guesstimate at size)
            var results = new List<List<IntPoint>>(interiorNodes.Length * 3);

            //Create short overlapping paths from each interior node to every pair of adjacent nodes
            foreach (var interior in interiorNodes)
            {
                foreach (var ba in interior.OutwardEdges.Where(tree.Contains))
                {
                    var ba1 = ba;
                    foreach (var bc in interior.OutwardEdges.Where(e => tree.Contains(e) && !e.Equals(ba1)))
                    {
                        var a = ba.End;
                        var b = interior;
                        var c = bc.End;

                        results.Add(new List<IntPoint> {
                            ToPoint(To2(a.Position)),
                            ToPoint(To2(b.Position)),
                            ToPoint(To2(c.Position)),
                        });
                    }
                }
            }

            return results;
        }

        private static Tree<TV, TE> TreeFromVertices<TV, TE>(ISet<TV> vertices)
            where TV : IVertex<TV, TE>
            where TE : IEdge<TV, TE>
        {
            return new Tree<TV, TE>(
                vertices,
                vertices.SelectMany(v => v.OutwardEdges).Where(e => vertices.Contains(e.End))
            );
        }

        #region graph vector2/vector3 conversion
        public static Vector3 To3(Vector2 v)
        {
            return v.XY_(0);
        }

        public static Vector2 To2(Vector3 v)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator (Justification: Checking for a known hardcoded value)
            if (v.Z != 0)
                throw new InvalidOperationException("Z element must be zero to be converted into vector2");

            return v.XY();
        }
        #endregion

        #region clipper point/vector conversion
        public const float POINT_SCALE = 1000f;
        public static IntPoint ToPoint(Vector2 p)
        {
            return new IntPoint((int)(p.X * POINT_SCALE), (int)(p.Y * POINT_SCALE));
        }

        public static Vector2 ToVector(IntPoint p)
        {
            return new Vector2(p.X / POINT_SCALE, p.Y / POINT_SCALE);
        }
        #endregion
    }
}
