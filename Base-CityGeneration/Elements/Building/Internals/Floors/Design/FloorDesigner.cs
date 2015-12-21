using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using CGAL_StraightSkeleton_Dotnet;
using EpimetheusPlugins.Scripts;
using JetBrains.Annotations;
using Myre.Collections;
using SharpYaml.Serialization;
using SwizzleMyVectors.Geometry;
#if DEBUG
using PrimitiveSvgBuilder;
#endif

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
            get { return _rooms; }
        }

        private readonly IValueGenerator _minimumRegionSize;
        public IValueGenerator MinimumRegionSize
        {
            get { return _minimumRegionSize; }
        }
        #endregion

        private FloorDesigner(Dictionary<string, string> tags, Guid id, string description, IReadOnlyCollection<ISpaceSpecProducer> rooms, IValueGenerator minimumRegionSize)
        {
            Tags = tags;
            Id = id;
            Description = description;

            _rooms = rooms;
            _minimumRegionSize = minimumRegionSize;
        }

        public FloorPlan Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<FloorplanRegion.Side> footprint)
        {
#if DEBUG
            var svg = new SvgBuilder(10);
#endif

            //Generate a floor skeleton to lay hallways along and subdivide the floor into regions
            var regions = GenerateRegions(footprint, random, metadata, 10).ToArray(); //TODO: [Floorplan] Parameterize error!

            //Assign *required* rooms to the regions they are most likely to be satisfied in
            AssignRooms(_rooms.SelectMany(r => r.Produce(true, random, metadata)).ToArray(), regions, random, metadata, true);
            AssignRooms(_rooms.SelectMany(r => r.Produce(false, random, metadata)).ToArray(), regions, random, metadata, false);

#if DEBUG
            foreach (var region in regions)
            {
                svg.Outline(region.Points.ToArray());

                var oabb = (Vector2[])region.OABR.Points(new Vector2[4]);
                svg.Outline(oabb, "red");
                svg.Text(region.AssignedSpaces.Count.ToString(), region.Points.Aggregate((a, b) => a + b) / region.Shape.Count);
            }
#endif

            
            //Connect external doors to hallway
            //Connect vertical features to hallway
            //  - Either create them on the corridor
            //  - Or create a new corridor to the vertical

            //Split space into regions (bounded by hallways)

            //Place rooms and shuffle to maximise satisfied constraints (this may be a little complex!)

            //If a space is passthrough merge it into adjacent hallways and expand it to fill space

#if DEBUG
            Console.WriteLine(svg.ToString());
#endif

            return null;
        }

        private static void AssignRooms(IEnumerable<BaseSpaceSpec> requiredSpecs, IEnumerable<FloorplanRegion> regions, Func<double> random, INamedDataCollection metadata, bool required)
        {
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

                //If no region has been selected either throw (required region) or continue (optional region)
                if (!bestRegion.HasValue || bestRegion.Value.Key <= 0.01f)
                {
                    if (!required)
                        continue;
                    throw new DesignFailedException(string.Format("Cannot find a region which satisfies all constraints \"{0}\" to", spec.Id));
                }

                //Sanity check
                if (float.IsNaN(bestRegion.Value.Key))
                    throw new InvalidOperationException("Heuristic score for floorplan region is NaN");

                bestRegion.Value.Value.Add(spec, random, metadata);
            }
        }

        private static float ScoreRegionForSpec(FloorplanRegion region, BaseSpaceSpec spec, Func<double> random, INamedDataCollection metadata)
        {
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
        /// <param name="footprint"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="areaErrorTolerance"></param>
        /// <returns></returns>
        private IEnumerable<FloorplanRegion> GenerateRegions(IReadOnlyList<FloorplanRegion.Side> footprint, Func<double> random, INamedDataCollection metadata, float areaErrorTolerance)
        {
            var root = new FloorplanRegion(footprint);

            using (var straightSkeleton = StraightSkeleton.Generate(root.Points.ToArray()))
            {
                //Slice the floorplan up by the lines of the straight skeleton, do not allow any polygons which are smaller than the limit
                var regions = new List<FloorplanRegion> { root };
                foreach (var edge in straightSkeleton.Skeleton.OrderByPoint(a => a.Start.Position).ThenByPoint(a => a.End.Position))
                {
                    var sliceLine = new Ray2(edge.Start.Position, edge.End.Position - edge.Start.Position);

                    //slice each region by each skeleton corner
                    var wip = new List<FloorplanRegion>();
                    foreach (var region in regions)
                    {
                        //todo: throw new NotImplementedException("Prefer cut which does not intersect a window, do not allow cuts which intersect a door");

                        var sliced = region.Slice(sliceLine);
                        if (sliced.Any(a => a.Area < _minimumRegionSize.SelectFloatValue(random, metadata)))
                            wip.Add(region);
                        else
                            wip.AddRange(sliced);
                    }

                    regions = wip;
                }


                return regions
                    .SelectMany(a => a.RecursiveReduceError(areaErrorTolerance));
            }
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

            public object MinimumRegionSize { get; [UsedImplicitly]set; }

            public FloorDesigner Unwrap()
            {
                return new FloorDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description,
                    Rooms.Select(a => a.Unwrap()).ToArray(),
                    BaseValueGeneratorContainer.FromObject(MinimumRegionSize ?? 9)
                );
            }
        }
        #endregion
    }
}
