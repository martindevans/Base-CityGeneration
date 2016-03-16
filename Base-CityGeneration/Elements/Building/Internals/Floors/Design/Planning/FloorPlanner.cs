using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Scripts;
using HandyCollections.Extensions;
using Myre.Collections;
using PrimitiveSvgBuilder;

using Face = Base_CityGeneration.Datastructures.HalfEdge.Face<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using HalfEdge = Base_CityGeneration.Datastructures.HalfEdge.HalfEdge<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Vertex = Base_CityGeneration.Datastructures.HalfEdge.Vertex<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;
using Mesh = Base_CityGeneration.Datastructures.HalfEdge.Mesh<Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanVertexTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanHalfEdgeTag, Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning.FloorplanFaceTag>;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    /// <summary>
    /// Internal implementation of the logic used in FloorDesigner.
    /// FloorDesigner is mostly concerned with gathering up the data needed and formatting it to pass into here for actual work to happen
    /// </summary>
    internal class FloorPlanner
    {
        private readonly Func<double> _random;
        private readonly INamedDataCollection _metadata;
        private readonly Func<KeyValuePair<string, string>[], Type[], ScriptReference> _finder;
        private readonly float _wallThickness;
        private readonly WallGrowthParameters _wallGrowthParameters;

        public FloorPlanner(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, float wallThickness, WallGrowthParameters wallGrowthParameters)
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
        }

        public FloorPlanBuilder Plan(Region region, IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            var builder = new FloorPlanBuilder(region.Points.ToArray());

            PlanRegion(builder, region, overlappingVerticals, startingVerticals, spaces);

            return builder;
            throw new NotImplementedException();
        }

        private void PlanRegion(FloorPlanBuilder builder, Region region, IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            //Grow floorplan for region
            var map = new GrowthMap(region.Points.ToArray(), overlappingVerticals, _random, _metadata, _wallGrowthParameters).Grow();

            //Remove faces which are too small
            RemoveFaces(map, a => a.Tag.AngularDeviation > 0.5, a => a.Tag.Mergeable, ScoreMergeCandidate, MergeTags);

            //todo: remove temp visualisation code
            var svg = new SvgBuilder(10);
            foreach (var face in map.Faces)
            {
                string col;
                float? value = null;
                if (!face.Tag.Mergeable)
                    col = "darkgray";
                else
                {
                    //Angular variance highlighting
                    value = face.Tag.AngularDeviation;
                    col = string.Format("rgb({0},0,255)", (int)Math.Min(255, 255f * value.Value * 3));
                    if (value < 0.3)
                        col = "white";

                    //bool av = face.Tag.AngularDeviation > 0.25;
                    //bool lv = face.Tag.LengthDeviation > 0.9;
                    //if (lv || av)
                    //    col = string.Format("rgb(255,0,255)");
                }

                svg.Outline(face.Vertices.Select(a => a.Position).ToArray(), stroke: "none", fill: col);
                if (value.HasValue)
                    svg.Text(value.Value.ToString("#.###"), face.Vertices.Select(a => a.Position).Aggregate((a, b) => a + b) / face.Vertices.Count(), fontSize: 10);
            }
            foreach (var edge in map.HalfEdges.Where(a => a.IsPrimaryEdge))
            {
                svg.Line(edge.StartVertex.Position, edge.EndVertex.Position, 1, "black");
            }
            foreach (var vertex in map.Vertices)
                svg.Circle(vertex.Position, 0.2f, "black");
            Console.WriteLine(svg.ToString());

            //todo: order specs by constraints (most difficult to solve first), assign specs to spaces generated (best fit)
            //todo: connectivity (doors + corridors)
            //todo: recursive for groups
        }

        private static float ScoreMergeCandidate(IReadOnlyList<Vertex> vertices)
        {
            //return -vertices.Count;

            return FloorplanFaceTag.CalculateAngularVariance(vertices);
        }

        #region removing/merging faces
        private static FloorplanFaceTag MergeTags(FloorplanFaceTag a, FloorplanFaceTag b)
        {
            Contract.Requires(a != null && a.Mergeable);
            Contract.Requires(b != null && b.Mergeable);

            return new FloorplanFaceTag(true);
        }

        /// <summary>
        /// Remove certain faces from the mesh
        /// </summary>
        /// <param name="mesh">The mesh to modify</param>
        /// <param name="predicate">A predicate function for selecting faces which need removing</param>
        /// <param name="mergeCandidate">A predicate which decides if a face may be deleted as part of a merge</param>
        /// <param name="score">A score function for the potential shape of merged faces (best scoring potential shape will be created)</param>
        /// <param name="merge">A method for merging tags</param>
        private static void RemoveFaces(Mesh mesh, Func<Face, bool> predicate, Func<Face, bool> mergeCandidate, Func<IReadOnlyList<Vertex>, float> score, Func<FloorplanFaceTag, FloorplanFaceTag, FloorplanFaceTag> merge)
        {
            Contract.Requires(mesh != null);
            Contract.Requires(predicate != null);
            Contract.Requires(score != null);

            //Remove faces which pass the predicates (keep repeating until we find no more to merge)
            int merged;
            do
            {
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
                    var face = MergeRoomWithNeighbour(mesh, candidate, mergeCandidate, score, out removed);

                    if (face != null)
                    {
                        merged++;

                        //Create a new tag (by recursive merging of tags)
                        face.Tag = removed.Select(a => a.Tag).Aggregate(merge);
                    }
                }
            } while (merged > 0);

            //Remove edges which are floating in space, not attached to a face
            mesh.RemoveDisconnectedEdges();

            //Remove vertices which are floating in space, disconnected from all edges
            mesh.RemoveDisconnectedVertices();

            //Removed vertices which lie on a perfectly straight line between 2 faces (linear reduction)
            mesh.SimplifyFaces();
        }

        /// <summary>
        /// Remove the given room from the plan by merging it with an adjacent room
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="faceToRemove">Room to remove</param>
        /// <param name="mergeCandidate"></param>
        /// <param name="scoreFunc">Score function - Given a candidate set of vertices for a new room, calculate a score for this room (higher is better)</param>
        /// <param name="merged"></param>
        private static Face MergeRoomWithNeighbour(Mesh mesh, Face faceToRemove, Func<Face, bool> mergeCandidate, Func<IReadOnlyList<Vertex>, float> scoreFunc, out IEnumerable<Face> merged)
        {
            //find the best neighbouring room
            var bestShape = faceToRemove
                .Edges
                .Where(e => e.Pair.Face != null)
                .Where(e => mergeCandidate(e.Pair.Face))
                .Select(MergedFacesShape)
                .MaxItem(scoreFunc);

            //Nothing to merge with? give up!
            if (bestShape == null)
            {
                merged = null;
                return null;
            }

            //Walk edges of this new shape and find all the faces *inside* (i.e. one we're replacing)
            var faces = new HashSet<Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>();
            for (var i = 0; i < bestShape.Count; i++)
            {
                var a = bestShape[i];
                var b = bestShape[(i + 1) % bestShape.Count];
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

            //Ensure that all edges of the faces being removed are accounted for
            //If this merge would create an island of one or more faces (i.e. a hole in the new face) that's invalid and we should bail
            bool edgesAllUsed = faces.SelectMany(f => f.Edges)
                 .All(e => commonEdges.Contains(e) || (bestShape.Contains(e.StartVertex) && bestShape.Contains(e.EndVertex)));

            //Bail!
            if (!edgesAllUsed)
            {
                merged = null;
                return null;
            }

            //Delete all the faces and common edges
            mesh.Delete(faces);
            mesh.Delete(commonEdges);

            //Create a new face
            var newFace = mesh.GetOrConstructFace(bestShape);

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
