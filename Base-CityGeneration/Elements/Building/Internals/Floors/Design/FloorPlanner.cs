using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures.HalfEdge;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.SpaceMapping;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using Myre.Collections;
using Placeholder.AI.Pathfinding.SpanningTree;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    internal class FloorPlanner
    {
        private readonly IReadOnlyCollection<ISpaceSpecProducer> _rooms;
        private readonly IValueGenerator _regionErrorTolerance;
        private readonly Func<double> _random;
        private readonly INamedDataCollection _metadata;
        private readonly Func<KeyValuePair<string, string>[], Type[], ScriptReference> _finder;
        private readonly IReadOnlyList<BasePolygonRegion<FloorplanRegion, Section>.Side> _footprint;
        private readonly float _wallThickness;
        private readonly FloorPlan _plan;

        private readonly ISpanner _spanner = new Kruskal();

        public FloorPlanner(IReadOnlyCollection<ISpaceSpecProducer> rooms, IValueGenerator regionErrorTolerance, Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<BasePolygonRegion<FloorplanRegion, Section>.Side> footprint, float wallThickness)
        {
            Contract.Requires(rooms != null);
            Contract.Requires(regionErrorTolerance != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(footprint != null);

            _rooms = rooms;
            _regionErrorTolerance = regionErrorTolerance;
            _random = random;
            _metadata = metadata;
            _finder = finder;
            _footprint = footprint;
            _wallThickness = wallThickness;

            _plan = new FloorPlan(_footprint.Select(a => a.Start).ToArray());
        }

        [ContractInvariantMethod]
        private void ObjectInvariants()
        {
            Contract.Invariant(_rooms != null);
            Contract.Invariant(_regionErrorTolerance != null);
            Contract.Invariant(_random != null);
            Contract.Invariant(_metadata != null);
            Contract.Invariant(_finder != null);
            Contract.Invariant(_footprint != null);
        }

        /// <summary>
        /// Plan a floor
        /// </summary>
        /// <param name="overlappingVerticals">verticals which started on a lower floor and overlap this one (shape of footprint)</param>
        /// <param name="startingVerticals">verticals which start on this floor (selection data, must be placed somewhere in the floorplan)</param>
        /// <returns></returns>
        public FloorPlan Plan(IList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals)
        {
            //We will recursively subdivide this root node, assigning more spaces as we go
            var root = new FloorplanRegion(_footprint);

            //Assign required and optional spaces to the root node, optional spaces will be pruned out as we subdivide
            AssignRooms(_rooms.SelectMany(r => r.Produce(true, _random, _metadata)), new[] { root }, true);
            AssignRooms(_rooms.SelectMany(r => r.Produce(false, _random, _metadata)), new[] { root }, false);

            //Recursively layout the spaces in the root
            RecursiveDesignRegion(root);

            return _plan;
        }

        #region region layout
        /// <summary>
        /// Given a region (with spaces assigned to it) lay out the rooms in the region, or split the region into smaller regions and recursively design those regions
        /// </summary>
        /// <param name="region">The region to lay out rooms in or split into smaller regions</param>
        /// <returns></returns>
        private void RecursiveDesignRegion(FloorplanRegion region)
        {
            Contract.Requires(region != null);

            //Split region up into sub regions
            var regions = GenerateRegions(region, _regionErrorTolerance.SelectFloatValue(_random, _metadata)).ToArray();

            //Assign rooms to the regions they are most likely to be satisfied in
            //Required...
            AssignRooms(((ISpaceSpecProducer)region).Produce(true, _random, _metadata), regions, true);
            //Optional...
            AssignRooms(((ISpaceSpecProducer)region).Produce(false, _random, _metadata), regions, false);

            //Physically lay out rooms within each region
            foreach (var childRegion in regions)
                LayoutRegion(childRegion);
        }

        /// <summary>
        /// Layout spaces into region. If any space is a group of more spaces this will recurse to RecursiveDesignRegion for that group
        /// </summary>
        /// <param name="region">The region to lay out spaces within</param>
        private void LayoutRegion(FloorplanRegion region)
        {
            Contract.Requires(region != null);

            //Layout spaces in this region
            var layoutMesh = region.LayoutSpaces(_random, _metadata);

            //Check connecitivity, add in corridors as necessary
            EnsureConnectivity(region, layoutMesh);

            //Convert the halfedge mesh representation of the rooms into a simple set of shapes associated with space-specs
            var spaces = ConvertMeshToSpecs(layoutMesh);

            //At this point all spaces are either leaves (i.e. rooms) or inner nodes (i.e. groups of more spaces)
            var rooms = spaces.Where(a => a.Value is RoomSpec);
            var groups = spaces.Where(a => a.Value is GroupSpec);

            //Sanity check
            if (spaces.Select(a => a.Value).Any(a => !(a is RoomSpec || a is GroupSpec)))
                throw new InvalidOperationException();

            

            //Add non-group rooms to plan
            foreach (var room in rooms)
            {
                var scripts = ((RoomSpec)room.Value).Tags.SelectScript(_random, _finder, typeof(IRoom));

                _plan.AddRoom(room.Key, _wallThickness, new[] { scripts.HasValue ? scripts.Value.Script : null }, room.Value.Id);
            }

            //Expand groups
            foreach (var group in groups)
            {
                //Generate the shape of this region (as if it were a room)
                var shape = _plan.TestRoom(group.Key, shrink: false);
                if (!shape.Any())
                    throw new DesignFailedException(string.Format("Failed to create sub region for group \"{0}\"", group.Value.Id));

                var sub = region.SubRegion(shape.Single());
                AssignRooms(((GroupSpec)group.Value).Rooms.SelectMany(r => r.Produce(true, _random, _metadata)), new[] { sub }, true);
                AssignRooms(((GroupSpec)group.Value).Rooms.SelectMany(r => r.Produce(false, _random, _metadata)), new[] { sub }, false);

                RecursiveDesignRegion(sub);
            }
        }

        private static IEnumerable<KeyValuePair<IReadOnlyList<Vector2>, BaseSpaceSpec>> ConvertMeshToSpecs(Mesh<SpaceCornerVertex, SpaceWall, SpaceFace> layoutMesh)
        {
            return from face in layoutMesh.Faces
                   where face.Tag != null
                   let vertices = face.Vertices.Select(v => v.Position).ToArray()
                   let spec = face.Tag.Spec
                   select new KeyValuePair<IReadOnlyList<Vector2>, BaseSpaceSpec>(vertices, spec);
        }

        /// <summary>
        /// Add corridors into the region to connect things together
        /// </summary>
        /// <param name="region"></param>
        /// <param name="mesh"></param>
        private void EnsureConnectivity(FloorplanRegion region, Mesh<SpaceCornerVertex, SpaceWall, SpaceFace> mesh)
        {
            //Vertices which lie on the edge of the shape are connected to a half edge with no face. Set all these vertices as candidates to remove
            var toRemove = new HashSet<Vertex<SpaceCornerVertex, SpaceWall, SpaceFace>>(mesh.HalfEdges.Where(a => a.Face == null).SelectMany(a => new[] { a.EndVertex, a.Pair.EndVertex }));

            //If a face is bordered *entirely* by these vertices, remove them all from the set
            foreach (var face in mesh.Faces)
                if (face.Vertices.All(toRemove.Contains))
                    toRemove.ExceptWith(face.Vertices);

            //order by point to get these vertices in an arbitrary deterministic order
            var vertices = mesh.Vertices.Where(v => !toRemove.Contains(v)).OrderByPoint(a => a.Position);

            //Generate a spanning tree over this graph, that's the best place to lay corridors
            var span = _spanner.Span(vertices).ToArray();
            if (!span.Any())
                return;

            //throw new NotImplementedException("lay corridor along the spanning tree(s). Do not lay any sections which are along the edge of a walkable room, instead create a door into the room");
        }
        #endregion

        #region spec assignment to regions
        /// <summary>
        /// Given a load of space specs and a load of regions assign spaces to the region where they are most likely to be satisfied
        /// </summary>
        /// <param name="spaces">The spaces to assign to a region</param>
        /// <param name="regions">The regions to assign spaces to</param>
        /// <param name="required">indicates if these spaces *must* be assigned (if not, then spaces may be skipped)</param>
        /// <exception cref="DesignFailedException">Thrown if a space cannot be assigned to a region and required is true</exception>
        private static void AssignRooms(IEnumerable<BaseSpaceSpec> spaces, IEnumerable<FloorplanRegion> regions, bool required)
        {
            Contract.Requires(spaces != null);
            Contract.Requires(regions != null);

            //Assign every space spec to a region of space
            foreach (var spec in spaces)
            {
                //Find the "best" region to place this spec in (best guess)
                var bestRegion = regions

                    //Calculate score for each region
                    .Select(r => new KeyValuePair<float, FloorplanRegion>(ScoreRegionForSpec(r, spec), r))

                    //Order by area (descending). If we have a tie in scores we will take the region with the largest unassigned area
                    .OrderBy(r => -r.Value.UnassignedArea)

                    //We must consider the possibility no region can handle this spec, so we use nullables
                    .Cast<KeyValuePair<float, FloorplanRegion>?>()

                    //Aggregate the best spec
                    .Aggregate(default(KeyValuePair<float, FloorplanRegion>?), (a, b) => (a.HasValue && b.HasValue && a.Value.Key > b.Value.Key) ? a : b);

                //Early exit if there is no more space to assign (only for optional specs)
                if (!required && regions.All(a => a.UnassignedArea <= 0))
                    break;

                //If no region has been selected either throw (required region) or continue (optional region)
                if (!bestRegion.HasValue || bestRegion.Value.Key <= 0.0001f)
                {
                    if (!required)
                        continue;

                    //Find constraints with zero satisfaction chance
                    var unsat = spec.Constraints.Select(c => new { c, p = regions.Select(r => c.Requirement.AssessSatisfactionProbability(r)).Min() })
                        .Where(a => a.p <= 0)
                        .Select(a => a.c.Requirement.GetType().Name)
                        .ToArray();

                    throw new DesignFailedException(string.Format("Unsatisfiable constraints for \"{0}\" - [{1}]", spec.Id, string.Join(",", unsat)));
                }

                bestRegion.Value.Value.Add(spec, required);
            }
        }

        /// <summary>
        /// Generates a score for how likely a space is to have it's constraints satisfied into a given region
        /// </summary>
        /// <param name="region">Region to estimate probability of satisfaction in</param>
        /// <param name="spec">Space to estimate probability of satisfaction for</param>
        /// <returns>A strength weighted score for how likely the given spec is to have it's constrains satisfied into the given region</returns>
        private static float ScoreRegionForSpec(FloorplanRegion region, BaseSpaceSpec spec)
        {
            Contract.Requires(region != null);
            Contract.Requires(spec != null);

            //Sum up the total strength of all constraints
            var totalStrength = spec.Constraints.Sum(a => a.Strength);

            //Sum up the total chance of constraints being satisfied
            //Square the probability, to add more weight to low probabilities
            //Multiply by strength to form a weighted average
            var totalChance = (from constraint in spec.Constraints
                               let strength = constraint.Strength
                               let probability = constraint.Requirement.AssessSatisfactionProbability(region)
                               select probability * probability * strength)
                .Aggregate((a, b) => a * b);

            //Normalize weighted average
            return totalChance / totalStrength;
        }
        #endregion

        #region region generation
        /// <summary>
        /// Split a region up into smaller regions by reducing OABR error
        /// </summary>
        /// <param name="root"></param>
        /// <param name="areaErrorTolerance"></param>
        /// <returns></returns>
        private static IEnumerable<FloorplanRegion> GenerateRegions(FloorplanRegion root, float areaErrorTolerance)
        {
            Contract.Requires(root != null);

            return new List<FloorplanRegion> { new FloorplanRegion(root.Shape, root.OABR) }
                .SelectMany(a => a.RecursiveReduceError(areaErrorTolerance))
                .ToArray();
        }
        #endregion
    }
}