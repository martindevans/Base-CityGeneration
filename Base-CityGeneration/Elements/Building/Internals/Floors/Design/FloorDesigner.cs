using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Utilities.Numbers;
using CGAL_StraightSkeleton_Dotnet;
using EpimetheusPlugins.Extensions;
using EpimetheusPlugins.Procedural.Utilities;
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

        public FloorPlan Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<Vector2> footprint)
        {
#if DEBUG
            var svg = new SvgBuilder(10);
#endif

            //Generate set of required spaces
            var requiredSpecs = _rooms.SelectMany(r => r.Produce(true, random, metadata)).ToArray();

            //Generate a floor skeleton to lay hallways along and subdivide the floor into regions
            var regions = GenerateRegions(footprint, random, metadata, 10).ToArray(); //TODO: Parameterize error!

            //Assign rooms to the regions they are most likely to be satisfied in
            AssignRooms(requiredSpecs, regions, random, metadata);

#if DEBUG
            foreach (var region in regions)
            {
                svg.Outline(region.Shape);

                var oabb = (Vector2[])region.OABR.Points(new Vector2[4]);
                svg.Outline(oabb, "red");
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

            return new FloorPlan(footprint);
        }

        private static void AssignRooms(IEnumerable<BaseSpaceSpec> requiredSpecs, IEnumerable<FloorplanRegion> regions, Func<double> random, INamedDataCollection metadata)
        {
            //Assign every space spec to a region of space
            foreach (var spec in requiredSpecs)
            {
                //Find the "best" region to place this spec in (best guess)
                var bestRegion = regions
                    .Select(r => new KeyValuePair<float, FloorplanRegion>(ScoreRegionForSpec(r, spec), r))
                    .Aggregate((a, b) => a.Key > b.Key ? a : b)
                    .Value;
                bestRegion.Add(spec, random, metadata);
            }
        }

        private static float ScoreRegionForSpec(FloorplanRegion region, BaseSpaceSpec spec)
        {
            return 1;
        }

        /// <summary>
        /// Split the floor up into regions of space
        /// </summary>
        /// <param name="footprint"></param>
        /// <param name="random"></param>
        /// <param name="metadata"></param>
        /// <param name="areaErrorTolerance"></param>
        /// <returns></returns>
        private IEnumerable<FloorplanRegion> GenerateRegions(IReadOnlyList<Vector2> footprint, Func<double> random, INamedDataCollection metadata, float areaErrorTolerance)
        {
            using (var straightSkeleton = StraightSkeleton.Generate(footprint))
            {
                //Slice the floorplan up by the lines of the straight skeleton, do not allow any polygons which are smaller than the limit
                var parts = new List<IReadOnlyList<Vector2>> { footprint };
                foreach (var edge in straightSkeleton.Skeleton)
                {
                    var sliceLine = new Ray2(edge.Start.Position, edge.End.Position - edge.Start.Position);

                    var wip = new List<IReadOnlyList<Vector2>>();
                    foreach (var part in parts)
                    {
                        var sliced = part.SlicePolygon(sliceLine);
                        if (sliced.Any(a => a.Area() < _minimumRegionSize.SelectFloatValue(random, metadata)))
                            wip.Add(part);
                        else
                            wip.AddRange(sliced);
                    }

                    parts = wip;
                }

                return parts
                    .Select(a => new FloorplanRegion(a))
                    .SelectMany(a => a.ReduceError(areaErrorTolerance));
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
            serializer.Settings.RegisterTagMapping("Exterior", typeof(Exterior.Container));
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
