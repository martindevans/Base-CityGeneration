using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using ClipperRedux;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using HandyCollections.Heap;
using JetBrains.Annotations;
using Myre.Collections;
using Placeholder.AI.Pathfinding.SpanningTree;
using Face = Base_CityGeneration.Datastructures.HalfEdge.Face<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using HalfEdge = Base_CityGeneration.Datastructures.HalfEdge.HalfEdge<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Vertex = Base_CityGeneration.Datastructures.HalfEdge.Vertex<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Mesh = Base_CityGeneration.Datastructures.HalfEdge.Mesh<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Vector2 = System.Numerics.Vector2;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    /// <summary>
    /// Internal implementation of the logic used in FloorDesigner.
    /// FloorDesigner is mostly concerned with gathering up the data needed and formatting it to pass into here for actual work to happen
    /// </summary>
    internal class FloorPlanner
    {
        #region helper classes
        public class MergingParameters
        {
            public IValueGenerator AngularWeight { get; set; }
            public IValueGenerator AngularThreshold { get; set; }

            public IValueGenerator ConvexWeight { get; set; }
            public IValueGenerator ConvexThreshold { get; set; }

            public IValueGenerator AreaWeight { get; set; }
            public IValueGenerator AreaThreshold { get; set; }

            public IValueGenerator AreaCutoff { get; set; }

            private MergingParameters(IValueGenerator angularWeight, IValueGenerator angularThreshold, IValueGenerator convexWeight, IValueGenerator convexThreshold, IValueGenerator areaWeight, IValueGenerator areaThreshold, IValueGenerator areaCutoff)
            {
                AngularWeight = angularWeight;
                AngularThreshold = angularThreshold;
                ConvexWeight = convexWeight;
                ConvexThreshold = convexThreshold;
                AreaWeight = areaWeight;
                AreaThreshold = areaThreshold;
                AreaCutoff = areaCutoff;
            }

            internal class Container
                : IUnwrappable<MergingParameters>
            {
                public struct MergeParamPair
                {
                    public object Weight { get; [UsedImplicitly] set; }
                    public object Threshold { get; [UsedImplicitly] set; }

                    public MergeParamPair(object weight, object threshold)
                        : this()
                    {
                        Weight = weight;
                        Threshold = threshold;
                    }
                }

                public struct MergeParamPairWithCutoff
                {
                    public object Weight { get; [UsedImplicitly] set; }
                    public object Threshold { get; [UsedImplicitly] set; }
                    public object Cutoff { get; [UsedImplicitly] set; }

                    public MergeParamPairWithCutoff(object weight, object threshold, object cutoff)
                        : this()
                    {
                        Weight = weight;
                        Threshold = threshold;
                        Cutoff = cutoff;
                    }
                }

                public MergeParamPair? AngularDeviation { get; [UsedImplicitly] set; }
                public MergeParamPair? Convexity { get; [UsedImplicitly] set; }
                public MergeParamPairWithCutoff? Area { get; [UsedImplicitly] set; }

                public MergingParameters Unwrap()
                {
                    var defaultAngular = new MergeParamPair(0.4f, 0.5f);
                    var angular = AngularDeviation == null ? defaultAngular : new MergeParamPair(AngularDeviation.Value.Weight ?? defaultAngular.Weight, AngularDeviation.Value.Threshold ?? defaultAngular.Threshold);

                    var defaultConvex = new MergeParamPair(0.3f, 0.9f);
                    var convex = Convexity == null ? defaultConvex : new MergeParamPair(Convexity.Value.Weight ?? defaultConvex.Weight, Convexity.Value.Threshold ?? defaultConvex.Threshold);

                    var defaultArea = new MergeParamPairWithCutoff(0.3f, 100, 4);
                    var area = Area == null ? defaultArea : new MergeParamPairWithCutoff(Area.Value.Weight ?? defaultArea.Weight, Area.Value.Threshold ?? defaultArea.Threshold, Area.Value.Cutoff ?? defaultArea.Cutoff);

                    return new MergingParameters(
                        IValueGeneratorContainer.FromObject(angular.Weight),
                        IValueGeneratorContainer.FromObject(angular.Threshold),
                        IValueGeneratorContainer.FromObject(convex.Weight),
                        IValueGeneratorContainer.FromObject(convex.Threshold),
                        IValueGeneratorContainer.FromObject(area.Weight),
                        IValueGeneratorContainer.FromObject(area.Threshold),
                        IValueGeneratorContainer.FromObject(area.Cutoff)
                    );
                }

                public static MergingParameters UnwrapDefault(Container maybeContainer)
                {
                    return (maybeContainer ?? new Container()).Unwrap();
                }
            }
        }

        public class CorridorParameters
        {
            public IValueGenerator Width { get; set; }

            public CorridorParameters(IValueGenerator width)
            {
                Width = width;
            }

            internal class Container
                : IUnwrappable<CorridorParameters>
            {
                public object Width { get; [UsedImplicitly] set; }

                public CorridorParameters Unwrap()
                {
                    return new CorridorParameters(
                        IValueGeneratorContainer.FromObject(Width ?? new NormallyDistributedValue(1, 1.5f, 2, 0.25f))
                    );
                }

                public static CorridorParameters UnwrapDefault(Container maybeContainer)
                {
                    return (maybeContainer ?? new Container()).Unwrap();
                }
            }
        }
        #endregion

        #region fields and properties
        private readonly Func<double> _random;
        private readonly INamedDataCollection _metadata;
        private readonly Func<KeyValuePair<string, string>[], Type[], ScriptReference> _finder;
        private readonly float _wallThickness;
        private readonly WallGrowthParameters _wallGrowthParameters;
        private readonly MergingParameters _mergeParameters;
        private readonly CorridorParameters _corridorParameters;
        #endregion

        #region constructor
        public FloorPlanner(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, float wallThickness, WallGrowthParameters wallGrowthParameters, MergingParameters mergeParameters, CorridorParameters corridorParameters)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(wallGrowthParameters != null);

            _random = random;
            _metadata = metadata;
            _finder = finder;
            _wallThickness = wallThickness;
            _wallGrowthParameters = wallGrowthParameters;
            _mergeParameters = mergeParameters;
            _corridorParameters = corridorParameters;
        }
        #endregion

        public IFloorPlanBuilder Plan(Region region, IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            Contract.Requires(region != null);
            Contract.Requires(overlappingVerticals != null);
            Contract.Requires(startingVerticals != null);
            Contract.Requires(spaces != null);

            var floorplan = new GeometricFloorplan(region.Points.ToArray());
            PlanRegion(floorplan, region, overlappingVerticals, startingVerticals, spaces);
            return floorplan;
        }

        private const float POINT_SCALE = 1000f;
        private static IntPoint ToPoint(Vector2 p)
        {
            return new IntPoint((int)(p.X * POINT_SCALE), (int)(p.Y * POINT_SCALE));
        }

        private static Vector2 ToVector(IntPoint p)
        {
            return new Vector2(p.X / POINT_SCALE, p.Y / POINT_SCALE);
        }

        private void PlanRegion(IFloorPlanBuilder floorplan, Region region, IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            Contract.Requires(region != null);
            Contract.Requires(overlappingVerticals != null);
            Contract.Requires(startingVerticals != null);
            Contract.Requires(spaces != null);

            //Grow floorplan for region
            var map = new GrowthMap(region.Points.ToArray(), overlappingVerticals, _random, _metadata, _wallGrowthParameters).Grow();

            //Remove oddly shaped rooms
            ReduceFaces(map);

            //Insert corridors into the plan
            GenerateCorridors(floorplan, map, region);

            //Assign specs (Rooms|Groups) to spaces
            //todo: order specs by constraints (most difficult to solve first), assign specs to spaces generated (best fit)

            //Ensure connectivity graph
            //todo: connectivity (doors + corridors)

            //Insert faces which are rooms (i.e. not groups) into the floorplan
            foreach (var face in map.Faces)
            {
                //if (face.Tag.Spec is RoomSpec)
                {
                    var shape = face.Vertices.Select(a => a.Position).ToArray();
                    floorplan.Add(shape, _wallThickness);
                }
                //else if (face.Tag.Spec is GroupSpec)
                //{
                //    todo: recursive create group layout
                //    Extract subregion shapes from floorplan (a corridor may have clipped the shape, so we can't just use the face shape)
                //    Then recursive layout regions in the calculated shape
                //}
            }

            return;
        }

        private void GenerateCorridors(IFloorPlanBuilder builder, Mesh map, Region region)
        {
            //Narrow down to only the vertices which are not on the border of this region and then generate the spanning tree(s) to connect these vertices
            var vertices = map.Vertices.Where(v => v.Edges.All(e => e.Tag == null || !e.Tag.IsImpassable));
            var spanningTrees = vertices.SpanningTreeKruskal<Vertex, HalfEdge>();

            //Convert spanning trees into polygons (with zero width on the corridors)
            var outlines = (
                from tree in spanningTrees
                let outline = WalkTreeOutline(tree)
                select outline
            ).ToArray();

            //Grow corridors to full width
            var corridorWidth = _corridorParameters.Width.SelectFloatValue(_random, _metadata);

            var shapes = Clipper.OffsetPolygons(
                outlines.Select(o => o.Select(ToPoint).ToArray()).ToArray(),
                corridorWidth * POINT_SCALE * 0.5f,
                JoinType.Miter,
                2,
                false
            ).Select(s => s.Select(ToVector).ToArray()).ToArray();

            //intersect corridors with region footprint
            var regionShape = region.Points.Select(ToPoint).ToArray();
            var clipper = new Clipper();
            var results = new List<List<IntPoint>>(1);
            foreach (var shape in shapes)
            {
                clipper.Clear();
                results.Clear();

                clipper.AddPolygon(regionShape, PolyType.Clip);
                clipper.AddPolygon(shape.Select(ToPoint).ToArray(), PolyType.Subject);
                clipper.Execute(ClipType.Intersection, results);

                foreach (var result in results)
                    builder.Add(result.Select(ToVector).Clockwise(), _wallThickness);
            }
        }

        private static IReadOnlyList<Vector2> WalkTreeOutline(Tree<Vertex, HalfEdge> tree)
        {
            Contract.Requires(tree != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vector2>>() != null);

            var result = new List<Vector2>(tree.VertexCount * 2);

            //Select a random leaf node as the start point
            var leaf = tree.Vertices.First(v => v.Edges.Where(tree.Contains).Count() == 1);
            WalkTreeOutline(result, tree, leaf, null);

            return result;
        }

        private static void WalkTreeOutline(ICollection<Vector2> result, Tree<Vertex, HalfEdge> tree, Vertex node, Vertex parent)
        {
            Contract.Requires(result != null);

            result.Add(node.Position);
            foreach (var edge in tree.Edges.Where(e => e.ConnectsTo(node) && !e.ConnectsTo(parent)))
            {
                var childNode = edge.EndVertex.Equals(node) ? edge.StartVertex : edge.EndVertex;
                WalkTreeOutline(result, tree, childNode, node);

                result.Add(node.Position);
            }
        }

        #region removing/merging faces
        /// <summary>
        /// Apply merging rules to reduce faces (removing oddly shaped faces)
        /// </summary>
        private void ReduceFaces(Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> map)
        {
            //Calculate parameters for merging
            var angularWeight = _mergeParameters.AngularWeight.SelectFloatValue(_random, _metadata);
            var convexWeight = _mergeParameters.ConvexWeight.SelectFloatValue(_random, _metadata);
            var areaWeight = _mergeParameters.AreaWeight.SelectFloatValue(_random, _metadata);

            //Normalize weights into (0 -> 1 range)
            var totalWeight = (angularWeight + areaWeight + convexWeight);
            angularWeight /= totalWeight;
            convexWeight /= totalWeight;
            areaWeight /= totalWeight;

            var angularThreshold = _mergeParameters.AngularThreshold.SelectFloatValue(_random, _metadata);
            var convexityThreshold = _mergeParameters.ConvexThreshold.SelectFloatValue(_random, _metadata);
            var areaThreshold = _mergeParameters.AreaThreshold.SelectFloatValue(_random, _metadata);

            //Keep repeating this loop (tightening the boudns each time) until an iteration does no work (i.e. merges nothing)
            var iterations = 0;
            int workDone;
            do
            {
                workDone = 0;

                var angDeviationThreshold = Math.Pow(angularThreshold, 1f / (iterations + 1));
                var convThreshold = Math.Pow(convexityThreshold, iterations + 1);

                //Remove faces with angular deviation too high or convexity too low
                workDone += RemoveFaces(map,
                    a => a.Tag.AngularDeviation > angDeviationThreshold || a.Tag.Convexity < convThreshold,
                    a => a.Tag.Mergeable,
                    a =>
                    {
                        var invAdjArea = 1 / Math.Max(1, a.Select(v => v.Position).Area() - areaThreshold);
                        var invAngDev = 1 - FloorplanFaceTag.CalculateAngularDeviation(a);
                        var convexity = FloorplanFaceTag.CalculateConvexity(a);

                        return (angularWeight * invAngDev) + (convexWeight * convexity) + (areaWeight * invAdjArea);
                    },
                    MergeTags,
                    1
                );

                iterations++;

            } while (workDone > 0);

            //We've done our best to reduce faces by merging them with neighbours
            //It's possible some invalid faces were not merged (no good merge candidates)
            //Now we just discard those faces which are below the *area* threshold
            //Why not the other thresholds? It's easy to fix area, but essentially impossible to reliably fix the other two, so it would cut out too many rooms to treat those are hard cutoffs
            var areaCutoff = _mergeParameters.AreaCutoff.SelectFloatValue(_random, _metadata);
            var facesToRemove = map.Faces.Where(a => a.Tag.Area < areaCutoff).ToArray();
            foreach (var face in facesToRemove)
                map.Delete(face);
        }

        /// <summary>
        /// Merge 2 floorplan tags togehter
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static FloorplanFaceTag MergeTags(FloorplanFaceTag a, FloorplanFaceTag b)
        {
            Contract.Requires(a != null && a.Mergeable);
            Contract.Requires(b != null && b.Mergeable);

            //Choose the non null spec (if either are not null)
            ISpec spec = null;
            if (a.Spec != null ^ b.Spec != null)
                spec = a.Spec ?? b.Spec;
            else if (a.Spec != null && b.Spec != null)
                throw new NotImplementedException("Both faces in merge already have a spec assigned");

            return new FloorplanFaceTag(
                true,   //A and B must be mergeable (see contract) so therefore this must be mergeable
                spec
            );
        }

        /// <summary>
        /// Remove certain faces from the mesh
        /// </summary>
        /// <param name="mesh">The mesh to modify</param>
        /// <param name="predicate">A predicate function for selecting faces which need removing</param>
        /// <param name="mergeCandidate">A predicate which decides if a face may be deleted as part of a merge</param>
        /// <param name="score">A score function for the potential shape of merged faces (best scoring potential shape will be created)</param>
        /// <param name="merge">A method for merging tags</param>
        /// <param name="maxIterations">Max number of times we should repeat the process</param>
        /// <returns>The number of removed faces</returns>
        private static int RemoveFaces(Mesh mesh, Func<Face, bool> predicate, Func<Face, bool> mergeCandidate, Func<IEnumerable<Vertex>, float> score, Func<FloorplanFaceTag, FloorplanFaceTag, FloorplanFaceTag> merge, int maxIterations = int.MaxValue)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(predicate != null);
            Contract.Requires(mergeCandidate != null);
            Contract.Requires(score != null);
            Contract.Requires(merge != null);

            //Remove faces which pass the predicates (keep repeating until we find no more to merge)
            int merged;
            int removedCount = 0;
            int iteration = 0;
            do
            {
                iteration++;

                merged = 0;
                var candidates = mesh.Faces.Where(mergeCandidate).Where(predicate).ToArray();
                foreach (var candidate in candidates)
                {
                    //Skip over deleted candidates, but consider them a reason to do another try at the overall loop
                    if (candidate.IsDeleted)
                    {
                        merged++;
                        continue;
                    }

                    //Remove this face by merging it with a neighbour (score by -num vertices in merge)
                    IEnumerable<Face> removed;
                    var face = TryMerge(mesh, candidate, mergeCandidate, score, out removed);

                    if (face != null)
                    {
                        merged++;
                        removedCount += removed.Count();

                        //Create a new tag (by recursive merging of tags)
                        face.Tag = removed.Select(a => a.Tag).Aggregate(merge);
                    }
                }
            } while (merged > 0 && iteration < maxIterations);

            //Remove edges which are floating in space, not attached to a face
            mesh.RemoveDisconnectedEdges();

            //Remove vertices which are floating in space, disconnected from all edges
            mesh.RemoveDisconnectedVertices();

            //Removed vertices which lie on a perfectly straight line between 2 faces (linear reduction)
            mesh.SimplifyFaces(FloorplanHalfEdgeTag.Merge);

            //Remove the total count of removed faces
            return removedCount;
        }

        /// <summary>
        /// Try to merge the given face with any neighbour which is a merge candidate. Try merges in order of score (best to worst)
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="faceToRemove"></param>
        /// <param name="isMergeCandidate"></param>
        /// <param name="scoreFunc"></param>
        /// <param name="removed"></param>
        /// <returns></returns>
        private static Face TryMerge(Mesh mesh, Face faceToRemove, Func<Face, bool> isMergeCandidate, Func<IEnumerable<Vertex>, float> scoreFunc, out IEnumerable<Face> removed)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(faceToRemove != null);
            Contract.Requires(isMergeCandidate != null);
            Contract.Requires(scoreFunc != null);

            //What's the score of the face we're removing?
            var faceToRemoveScore = scoreFunc(faceToRemove.Vertices);

            //Find all connected faces which result in a better score (ordered by that better score)
            var heap = new MinHeap<KeyValuePair<float, IReadOnlyList<Vertex>>>((a, b) => a.Key.CompareTo(b.Key));
            heap.Add(from edge in faceToRemove.Edges
                     where edge.Pair.Face != null && isMergeCandidate(edge.Pair.Face)               //Ignore faces which do not exist, or are not candidates
                     let face = edge.Pair.Face
                     let faceScore = scoreFunc(face.Vertices)                                       //What's the score of this face we want to remove?
                     let shape = MergedFacesShape(edge)                                             //What would be the outline of the result of this merge?
                     let shapeScore = scoreFunc(shape)                                              //What's the score of the result of this merge
                     where (shapeScore > Math.Min(faceScore, faceToRemoveScore))                    //Does the merge actually improve the current situation for at least one of the faces?
                     select new KeyValuePair<float, IReadOnlyList<Vertex>>(shapeScore, shape));

            //Keep trying shapes until a merge is successful
            while (heap.Count > 0)
            {
                var candidate = heap.RemoveMin().Value;

                var face = MergeFaces(mesh, candidate, out removed);
                if (face != null)
                    return face;
            }

            //Nothing to merge with? give up!
            removed = null;
            return null;
        }

        /// <summary>
        /// Create a new face with the given set of vertices, deleting all faces which are within those bounds.
        /// Do not merge (returns null) if this would create topologically invalid shape
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="vertices"></param>
        /// <param name="merged"></param>
        /// <returns></returns>
        private static Face MergeFaces(Mesh mesh, IReadOnlyList<Vertex> vertices, out IEnumerable<Face> merged)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(vertices != null);

            //Walk edges of this new shape and find all the faces *inside* (i.e. one we're replacing)
            var faces = new HashSet<Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>();
            for (var i = 0; i < vertices.Count; i++)
            {
                var a = vertices[i];
                var b = vertices[(i + 1) % vertices.Count];
                var e = mesh.GetOrConstructHalfEdge(a, b);

                if (e.Face != null)
                    faces.Add(e.Face);
            }

            //Find common edges between all the faces we're deleting (i.e. edges we're deleting)
            var commonEdges = new HashSet<HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>(
                faces
                .SelectMany(f => f.Edges)
                .Where(e => faces.Contains(e.Face))
                .Where(e => faces.Contains(e.Pair.Face))
                .Distinct()
            );

            //Ensure that all edges of the faces being removed are accounted for.
            //If an edge is not in the new shape and not a common edge between the two shapes we must be forming an island somewhere!
            //Islands/holes are topologically invalid (at the moment, anyway) and we should bail from this merge
            if (!faces.SelectMany(f => f.Edges).All(e => commonEdges.Contains(e) || (vertices.Contains(e.StartVertex) && vertices.Contains(e.EndVertex))))
            {
                //Bail!
                merged = null;
                return null;
            }

            //Ensure that we don't visit a vertex twice (this is topologically valid, but generates very odd rooms)
            if (vertices.GroupBy(a => a).Any(a => a.Count() > 1))
            {
                //Bail!
                merged = null;
                return null;
            }

            //Delete all the faces and common edges
            mesh.Delete(faces);
            mesh.Delete(commonEdges);

            //Create a new face
            var newFace = mesh.GetOrConstructFace(vertices);

            merged = faces;
            return newFace;
        }

        /// <summary>
        /// Given an edge, return the shape of the face which would result from removing that edge and merging the two faces
        /// </summary>
        /// <param name="commonEdge"></param>
        /// <returns></returns>
        private static IReadOnlyList<Vertex> MergedFacesShape(
            HalfEdge commonEdge
            )
        {
            Contract.Requires(commonEdge != null);
            Contract.Requires(commonEdge.Face != null);
            Contract.Requires(commonEdge.Pair.Face != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>>() != null);
            Contract.Ensures(Contract.Result<IReadOnlyList<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>>().Count >= 3);

            //The two faces (by definition - this is the common edge between those faces)
            var left = commonEdge.Face;
            var right = commonEdge.Pair.Face;

            //Edges which are in both faces
            var commonEdges = new HashSet<HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>(left.Edges.Where(e => right.Equals(e.Pair.Face)));
            commonEdges.UnionWith(right.Edges.Where(e => left.Equals(e.Pair.Face)));

            //This is the final shape we have walked
            var shape = new List<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>();

            //Pick an arbitrary half edge which is not a common edge
            var startEdge = right.Edges.First(e => !commonEdges.Contains(e));
            var current = startEdge;
            do
            {
                //If we've found a common vertex we need to start walking around the other face
                if (commonEdges.Contains(current))
                {
                    current = current.Pair.Next;
                }
                else
                {
                    //Store up the start vertices of the edges as we walk around the shape
                    shape.Add(current.StartVertex);

                    //Move to next edge around this face
                    current = current.Next;
                }

            } while (!current.Equals(startEdge));

            Contract.Assume(shape.Count >= 3);
            return shape;
        }
        #endregion
    }
}
