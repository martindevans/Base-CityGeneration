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
            Func<IEnumerable<Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>, bool> removeUndersizedFace = faces => {

                //Find a single face to remove (area too small)
                var f = faces.FirstOrDefault(a => a.Area() < 10);
                if (f == null)
                    return false;

                //Remove this face by merging it with a neighbour (score by -num vertices in merge)
                return MergeRoomWithNeighbour(map, f, a => -a.Count);
            };
            removeUndersizedFace.Fixpoint(map.Faces);

            //Remove rooms which are too small
            var undersized = map.Faces.Where(a => a.Area() < 10);
            //todo: look through specs we want to fit into this plan and find the smallest, that's our min area
            // ^ consider just dolling out specs, and then merging spaces which have nothing assigned

            //todo: remove temp visualisation code
            var svg = new SvgBuilder(10);
            foreach (var face in map.Faces)
            {
                //Area highlighting
                //var col = undersized.Contains(face) ? "red" : "cornflowerblue";

                

                ////Angular variance highlighting
                //var variance = face.Tag.AngularDeviation;
                //var col = string.Format("rgb({0},0,255)", (int)Math.Min(255, 255f * variance * 2));
                //if (variance < 0.25)
                //    col = "white";

                ////Length highlighting
                //var variance = face.Tag.LengthDeviation;
                //var col = string.Format("rgb({0},0,255)", (int)Math.Min(255, 255f * variance * 10));
                //if (variance < 0.9)
                //    col = "white";

                //bool av = face.Tag.AngularDeviation > 0.25;
                //bool lv = face.Tag.LengthDeviation > 0.9;
                //var col = "none";
                //if (lv || av)
                //    col = string.Format("rgb(255,0,255)");

                var col = "cornflowerblue";
                svg.Outline(face.Vertices.Select(a => a.Position).ToArray(), stroke: "none", fill: col);
                //svg.Text(variance.ToString("##.##"), face.Vertices.Select(a => a.Position).Aggregate((a, b) => a + b) / face.Vertices.Count(), fontSize: 10);
            }
            foreach (var edge in map.HalfEdges.Where(a => a.IsPrimaryEdge))
                svg.Line(edge.StartVertex.Position, edge.EndVertex.Position, 1, "black");
            foreach (var vertex in map.Vertices)
                svg.Circle(vertex.Position, 0.2f, "black");
            Console.WriteLine(svg.ToString());

            //todo: order specs by constraints (most difficult to solve first), assign specs to spaces generated (best fit)
            //todo: connectivity (doors + corridors)
            //todo: recursive for groups
        }

        /// <summary>
        /// Remove the given room from the plan by merging it with an adjacent room
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="faceToRemove">Room to remove</param>
        /// <param name="scoreFunc">Score function - Given a candidate set of vertices for a new room, calculate a score for this room (higher is better)</param>
        private static bool MergeRoomWithNeighbour(
            Mesh<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> mesh,
            Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> faceToRemove,
            Func<IReadOnlyList<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>, float> scoreFunc
        )
        {
            //find the best neighbouring room
            var bestShape = faceToRemove
                .Edges
                .Where(e => e.Pair.Face != null)
                .Select(MergedFacesShape)
                .MaxItem(scoreFunc);

            //Walk edges of this new shape and find all the faces
            var faces = new HashSet<Face<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>>();
            for (var i = 0; i < bestShape.Count; i++)
            {
                var a = bestShape[i];
                var b = bestShape[(i + 1) % bestShape.Count];
                var e = mesh.GetOrConstructHalfEdge(a, b);

                faces.Add(e.Face);
            }

            ////Now delete both faces and create a new face
            ////todo: temp! this only works if the faces have just one common edge
            //if (bestEdge != null)
            //{
            //    mesh.Delete(bestEdge.Face);
            //    mesh.Delete(bestEdge.Pair.Face);
            //    mesh.Delete(bestEdge);
            //    mesh.GetOrConstructFace(bestShape);
            //    return true;
            //}

            return false;
        }

        /// <summary>
        /// Given an edge, return the shape of the face which would result from removing that edge and merging the two faces
        /// </summary>
        /// <param name="commonEdge"></param>
        /// <returns></returns>
        private static Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>[] MergedFacesShape(
            HalfEdge<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag> commonEdge
        )
        {
            Contract.Requires(commonEdge != null);
            Contract.Requires(commonEdge.Face != null);
            Contract.Requires(commonEdge.Pair.Face != null);
            //Contract.Ensures(Contract.Result<Vertex<FloorplanVertexTag, FloorplanHalfEdgeTag, FloorplanFaceTag>[]>() != null);

            var left = commonEdge.Face;
            var right = commonEdge.Pair.Face;
            var commonEdges = left.Edges.Where(e => right.Equals(e.Pair.Face));

            if (commonEdges.Count() > 1)
                throw new NotImplementedException();

            //todo: temp merging code for special case of a single common edge
            var lv = commonEdge.Next.Around.TakeWhile(a => !a.StartVertex.Equals(commonEdge.StartVertex));
            var rv = commonEdge.Pair.Next.Around.TakeWhile(a => !a.StartVertex.Equals(commonEdge.Pair.StartVertex));

            return lv.Concat(rv).Select(a => a.EndVertex).ToArray();
        }
    }
}
