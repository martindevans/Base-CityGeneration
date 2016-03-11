using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.Extensions;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Scripts;
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

            //Remove rooms which are too small
            var undersized = map.Faces.Where(a => a.Area() < 10);
            //todo: look through specs we want to fit into this plan and find the smallest, that's our min area
            // ^ consider just dolling out specs, and then merging spaces which have nothing assigned

            //todo: remove temp visualisation code
            var svg = new SvgBuilder(10);
            foreach (var face in map.Faces)
            {
                var col = VertexVariance(face.Vertices.Select(a => a.Position).ToArray()) > 1.75f ? "red" : "cornflowerblue";
                svg.Outline(face.Vertices.Select(a => a.Position).ToArray(), stroke: "none", fill: col);
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

        private float VertexVariance(IReadOnlyList<Vector2> vertices)
        {
            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;

            foreach (var avgDist in vertices.Select(v => vertices.Select(a => Vector2.Distance(a, v)).Sum() / vertices.Count))
            {
                min = Math.Min(avgDist, min);
                max = Math.Max(avgDist, max);
            }

            return max / min;
        }
    }
}
