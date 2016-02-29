using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning
{
    internal class FloorPlanner
    {
        private readonly Func<double> _random;
        private readonly INamedDataCollection _metadata;
        private readonly Func<KeyValuePair<string, string>[], Type[], ScriptReference> _finder;
        private readonly float _wallThickness;
        private readonly IValueGenerator _seedSpacing;
        private readonly float _internalAngleBisectChance;

        public FloorPlanner(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, float wallThickness, IValueGenerator seedSpacing, float bisectChance)
        {
            _random = random;
            _metadata = metadata;
            _finder = finder;
            _wallThickness = wallThickness;
            _seedSpacing = seedSpacing;
            _internalAngleBisectChance = bisectChance;
        }

        public FloorPlanBuilder Plan(Region region, IList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            var builder = new FloorPlanBuilder(region.Points.ToArray());

            PlanRegion(builder, region, overlappingVerticals, startingVerticals, spaces);

            return builder;
            throw new NotImplementedException();
        }

        private void PlanRegion(FloorPlanBuilder builder, Region region, IList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            var map = new GrowthMap(region.Points.ToArray(), _seedSpacing, _internalAngleBisectChance, _random, _metadata);
            map.Grow();

            //todo: seed along edges of region
            //todo: grow out from seeds (perpendicular to edge, or equally splitting an internal angle > 180)
            //todo: intersections form rooms, remove rooms which are too small (by merging into neighbouring rooms)
            //todo: order specs by constraints (most difficult to solve first), assign specs to spaces generated (best fit)
            //todo: connectivity (doors + corridors)
            //todo: recursive for groups
        }
    }
}
