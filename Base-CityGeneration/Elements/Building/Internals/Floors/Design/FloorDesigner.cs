using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Datastructures;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Rooms;
using Base_CityGeneration.Utilities;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Procedural.Utilities;
using EpimetheusPlugins.Scripts;
using JetBrains.Annotations;
using Myre.Collections;
using SharpYaml.Serialization;
using SwizzleMyVectors.Geometry;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorDesigner
    {
        #region field and properties
        public IEnumerable<KeyValuePair<string, string>> Tags { get; private set; }
        public Guid Id { get; private set; }
        public string Description { get; private set; }

        private readonly IReadOnlyCollection<ISpaceSpecProducer> _rooms;
        public IReadOnlyCollection<ISpaceSpecProducer> Rooms
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyCollection<ISpaceSpecProducer>>() != null);
                return _rooms;
            }
        }

        private readonly IValueGenerator _regionErrorTolerance;
        public IValueGenerator RegionErrorTolerance
        {
            get
            {
                Contract.Ensures(Contract.Result<IValueGenerator>() != null);
                return _regionErrorTolerance;
            }
        }
        #endregion

        private FloorDesigner(Dictionary<string, string> tags, Guid id, string description, IReadOnlyCollection<ISpaceSpecProducer> rooms, IValueGenerator regionErrorTolerance)
        {
            Contract.Requires(tags != null);
            Contract.Requires(description != null);
            Contract.Requires(rooms != null);
            Contract.Requires(regionErrorTolerance != null);

            Tags = tags;
            Id = id;
            Description = description;

            _rooms = rooms;
            _regionErrorTolerance = regionErrorTolerance;
        }

        public FloorPlan Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<BasePolygonRegion<FloorplanRegion, Section>.Side> footprint, float wallThickness)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(footprint != null && footprint.Count >= 3);
            Contract.Ensures(Contract.Result<FloorPlan>() != null);

            var plan = new FloorPlan(footprint.Select(a => a.Start).ToArray());

            //We will recursively subdivide this root node, assigning more spaces as we go
            var root = new FloorplanRegion(footprint);

            //Assign required and optional spaces to the root node, optional spaces will be pruned out as we subdivide
            AssignRooms(_rooms.SelectMany(r => r.Produce(true, random, metadata)), new [] { root }, random, metadata, true);
            AssignRooms(_rooms.SelectMany(r => r.Produce(false, random, metadata)), new[] { root }, random, metadata, false);

            //Recursively layout the spaces in the root
            DesignRegion(root, random, metadata, finder, plan, wallThickness, RegionErrorTolerance);

            return plan;
        }

        private static void DesignRegion(FloorplanRegion region, Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, FloorPlan plan, float wallThickness, IValueGenerator regionErrorTolerance)
        {
            Contract.Requires(region != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(plan != null);
            Contract.Requires(regionErrorTolerance != null);

            //Split region up into sub regions
            var regions = GenerateRegions(region, random, metadata, regionErrorTolerance.SelectFloatValue(random, metadata)).ToArray();

            //Assign rooms to the regions they are most likely to be satisfied in
            //Required...
            AssignRooms(((ISpaceSpecProducer)region).Produce(true, random, metadata), regions, random, metadata, true);
            //Optional...
            AssignRooms(((ISpaceSpecProducer)region).Produce(false, random, metadata), regions, random, metadata, false);

            //Physically lay out rooms within each region
            foreach (var childRegion in regions)
                LayoutRegion(childRegion, random, metadata, finder, plan, wallThickness, regionErrorTolerance);
        }

        private static void LayoutRegion(FloorplanRegion region, Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, FloorPlan plan, float wallThickness, IValueGenerator regionErrorTolerance)
        {
            Contract.Requires(region != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(plan != null);
            Contract.Requires(regionErrorTolerance != null);

            //Layout spaces in this region
            var spaces = region.LayoutSpaces(random, metadata);

            //At this point all spaces are either leaves (i.e. rooms) or inner nodes (i.e. groups of more spaces)
            var rooms = spaces.Where(a => a.Value is RoomSpec);
            var groups = spaces.Where(a => a.Value is GroupSpec);

            //Sanity check
            if (spaces.Select(a => a.Value).Any(a => !(a is RoomSpec || a is GroupSpec)))
                throw new InvalidOperationException();

            //Add non-group rooms to plan
            foreach (var room in rooms)
            {
                var scripts = ((RoomSpec)room.Value).Tags.SelectScript(random, finder, typeof(IRoom));

                plan.AddRoom(RoomShape(room.Key, region.OABR), wallThickness, new[] { scripts.HasValue ? scripts.Value.Script : null });
            }

            foreach (var group in groups)
            {
                //Generate the shape of this region (as if it were a room)
                var shape = plan.TestRoom(RoomShape(group.Key, region.OABR), shrink: false);
                if (!shape.Any())
                    throw new DesignFailedException(string.Format("Failed to create sub region for group \"{0}\"", group.Value.Id));

                var sub = region.SubRegion(shape.Single());
                AssignRooms(((GroupSpec)group.Value).Rooms.SelectMany(r => r.Produce(true, random, metadata)), new[] { sub }, random, metadata, true);
                AssignRooms(((GroupSpec)group.Value).Rooms.SelectMany(r => r.Produce(false, random, metadata)), new[] { sub }, random, metadata, false);

                DesignRegion(sub, random, metadata, finder, plan, wallThickness, regionErrorTolerance);
            }
        }

        private static IEnumerable<Vector2> RoomShape(BoundingRectangle key, OABR oabr)
        {
            return key.GetCorners().Select(oabr.ToWorld).ConvexHull();
        }

        private static void AssignRooms(IEnumerable<BaseSpaceSpec> requiredSpecs, IEnumerable<FloorplanRegion> regions, Func<double> random, INamedDataCollection metadata, bool required)
        {
            Contract.Requires(requiredSpecs != null);
            Contract.Requires(regions != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            //Assign every space spec to a region of space
            foreach (var spec in requiredSpecs)
            {
                //Find the "best" region to place this spec in (best guess)
                var bestRegion = regions

                    //Calculate score for each region
                    .Select(r => new KeyValuePair<float, FloorplanRegion>(ScoreRegionForSpec(r, spec, random, metadata), r))

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
                    var unsat = spec.Constraints.Select(c => new { c, p = regions.Select(r => c.Requirement.AssessSatisfactionProbability(r, random, metadata)).Min() })
                        .Where(a => a.p <= 0)
                        .Select(a => a.c.Requirement.GetType().Name)
                        .ToArray();

                    throw new DesignFailedException(string.Format("Unsatisfiable constraints for \"{0}\" - [{1}]", spec.Id, string.Join(",", unsat)));
                }

                ////Sanity check
                //if (float.IsNaN(bestRegion.Value.Key))
                //    throw new InvalidOperationException("Heuristic score for floorplan region is NaN");

                bestRegion.Value.Value.Add(spec, required, random, metadata);
            }
        }

        private static float ScoreRegionForSpec(FloorplanRegion region, BaseSpaceSpec spec, Func<double> random, INamedDataCollection metadata)
        {
            Contract.Requires(region != null);
            Contract.Requires(spec != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            //Sum up the total strength of all constraints
            var totalStrength = spec.Constraints.Sum(a => a.Strength.SelectFloatValue(random, metadata));

            //Sum up the total chance of constraints being satisfied
            //Square the probability, to add more weight to low probabilities
            //Multiply by strength to form a weighted average
            var totalChance = (from constraint in spec.Constraints
                               let strength = constraint.Strength.SelectFloatValue(random, metadata)
                               let probability = constraint.Requirement.AssessSatisfactionProbability(region, random, metadata)
                               select probability * probability * strength)
                .Aggregate((a, b) => a * b);

            //Normalize weighted average
            return totalChance / totalStrength;
        }

        /// <summary>
        /// Split the floor up into regions of space
        /// </summary>
        /// <param name="root"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="areaErrorTolerance"></param>
        /// <returns></returns>
        private static IEnumerable<FloorplanRegion> GenerateRegions(FloorplanRegion root, Func<double> random, INamedDataCollection metadata, float areaErrorTolerance)
        {
            Contract.Requires(root != null);
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);

            return new List<FloorplanRegion> { new FloorplanRegion(root.Shape, root.OABR) }
                .SelectMany(a => a.RecursiveReduceError(areaErrorTolerance))
                .ToArray();
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings {
                EmitTags = true,
            });

            //Root type
            serializer.Settings.RegisterTagMapping("Floorplan", typeof(Container));

            //space area types
            serializer.Settings.RegisterTagMapping("Room", typeof(RoomSpec.Container));
            serializer.Settings.RegisterTagMapping("Group", typeof(GroupSpec.Container));
            serializer.Settings.RegisterTagMapping("Repeat", typeof(RepeatSpec.Container));

            //Constraints
            serializer.Settings.RegisterTagMapping("ExteriorDoor", typeof(ExteriorDoor.Container));
            serializer.Settings.RegisterTagMapping("ExteriorWindow", typeof(ExteriorWindow.Container));
            serializer.Settings.RegisterTagMapping("Area", typeof(Area.Container));

            //Connections
            serializer.Settings.RegisterTagMapping("Not", typeof(Invert.Container));
            serializer.Settings.RegisterTagMapping("IdRef", typeof(IdRef.Container));
            serializer.Settings.RegisterTagMapping("Either", typeof(Either.Container));
            serializer.Settings.RegisterTagMapping("Tagged", typeof(TaggedRef.Container));
            serializer.Settings.RegisterTagMapping("RegexIdRef", typeof(RegexIdRef.Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            return serializer;
        }

        public static FloorDesigner Deserialize(TextReader reader)
        {
            return CreateSerializer().Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            //Collection of unused objects, helpful for writing scripts
            public List<object> Aliases { get; [UsedImplicitly] set; }

            // ReSharper disable once CollectionNeverUpdated.Global
            public Dictionary<string, string> Tags { get; [UsedImplicitly] set; }
            public string Id { get; [UsedImplicitly] set; }
            public string Description { get; [UsedImplicitly] set; }

            public ISpaceSpecProducerContainer[] Rooms { get; [UsedImplicitly] set; }

            public object RegionErrorTolerance { get; [UsedImplicitly]set; }

            public FloorDesigner Unwrap()
            {
                return new FloorDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description ?? "",
                    Rooms.Select(a => a.Unwrap()).ToArray(),
                    IValueGeneratorContainer.FromObject(RegionErrorTolerance ?? 0.05f)
                );
            }
        }
        #endregion
    }
}
