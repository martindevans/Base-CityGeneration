using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Design;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Utilities.Extensions;
using Base_CityGeneration.Utilities.Numbers;
using EpimetheusPlugins.Scripts;
using JetBrains.Annotations;
using Myre.Collections;
using SharpYaml.Serialization;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorDesigner
    {
        #region fields and properties
        #region metadata
        private readonly Dictionary<string, string> _tags;
        public Dictionary<string, string> Tags
        {
            get { return _tags; }
        }

        private readonly Guid _guid;
        public Guid Guid
        {
            get { return _guid; }
        }

        private readonly string _description;
        public string Description
        {
            get { return _description; }
        }
        #endregion

        private readonly IReadOnlyList<BaseSpaceSpec> _spaces;

        private readonly IValueGenerator _seedSpacing;
        private readonly IValueGenerator _seedChance;
        private readonly IValueGenerator _parallelCheckLength;
        private readonly IValueGenerator _parallelCheckWidth;
        private readonly IValueGenerator _parallelAngleThreshold;
        private readonly IValueGenerator _intersectionContinuationChance;
        #endregion

        #region constructor
        private FloorDesigner(Dictionary<string, string> tags, Guid guid, string description, IReadOnlyList<BaseSpaceSpec> spaces, IValueGenerator seedSpacing, IValueGenerator parallelCheckLength, IValueGenerator parallelCheckWidth, IValueGenerator parallelAngleThreshold, IValueGenerator intersectionContinuationChance, IValueGenerator seedChance)
        {
            _tags = tags;
            _guid = guid;
            _description = description;
            _spaces = spaces;

            _seedSpacing = seedSpacing;
            _seedChance = seedChance;
            _parallelCheckLength = parallelCheckLength;
            _parallelCheckWidth = parallelCheckWidth;
            _parallelAngleThreshold = parallelAngleThreshold;
            _intersectionContinuationChance = intersectionContinuationChance;
        }
        #endregion

        #region design
        public FloorPlanBuilder Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<Vector2> footprint, IReadOnlyList<IReadOnlyList<Subsection>> sections, float wallThickness, IList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<VerticalSelection> startingVerticals)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(footprint != null && footprint.Count >= 3);
            Contract.Requires(sections != null && sections.Count == footprint.Count);
            Contract.Requires(overlappingVerticals != null && Contract.ForAll(overlappingVerticals, o => o != null));
            Contract.Requires(startingVerticals != null && Contract.ForAll(startingVerticals, s => s != null));
            Contract.Ensures(Contract.Result<FloorPlanBuilder>() != null);

            var region = CreateRegion(footprint, sections);

            var planner = new FloorPlanner(random, metadata, finder, wallThickness, _seedSpacing, _parallelCheckLength, _parallelCheckWidth, _parallelAngleThreshold, _intersectionContinuationChance, _seedChance);
            return planner.Plan(region, overlappingVerticals, startingVerticals, _spaces);
        }

        /// <summary>
        /// Convert the shape and subsection information into a region
        /// </summary>
        /// <param name="footprint"></param>
        /// <param name="sections"></param>
        /// <returns></returns>
        private static Region CreateRegion(IReadOnlyList<Vector2> footprint, IReadOnlyList<IReadOnlyList<Subsection>> sections)
        {
            Contract.Requires(footprint != null && footprint.Count >= 3);
            Contract.Requires(sections != null && sections.Count == footprint.Count);

            //Assume they are the same length because contracts are awesome :D
            var sides = new List<Side>(footprint.Count);
            for (var i = 0; i < footprint.Count; i++)
            {
                var a = footprint[i];
                var b = footprint[(i + 1) % footprint.Count];
                var s = sections[i];
                sides.Add(new Side(s, a, b));
            }

            return new Region(sides);

        }
        #endregion

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            //Root type
            serializer.Settings.RegisterTagMapping("Floorplan", typeof(Container));

            //space area types
            serializer.Settings.RegisterTagMapping("Room", typeof(RoomSpec.Container));
            serializer.Settings.RegisterTagMapping("Group", typeof(GroupSpec.Container));
            serializer.Settings.RegisterTagMapping("Repeat", typeof(RepeatSpec.Container));

            ////Constraints
            //serializer.Settings.RegisterTagMapping("ExteriorDoor", typeof(ExteriorDoor.Container));
            //serializer.Settings.RegisterTagMapping("ExteriorWindow", typeof(ExteriorWindow.Container));
            //serializer.Settings.RegisterTagMapping("Area", typeof(Area.Container));

            ////Connections
            //serializer.Settings.RegisterTagMapping("Not", typeof(Invert.Container));
            //serializer.Settings.RegisterTagMapping("IdRef", typeof(IdRef.Container));
            //serializer.Settings.RegisterTagMapping("Either", typeof(Either.Container));
            //serializer.Settings.RegisterTagMapping("Tagged", typeof(TaggedRef.Container));
            //serializer.Settings.RegisterTagMapping("RegexIdRef", typeof(RegexIdRef.Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));

            return serializer;
        }

        public static FloorDesigner Deserialize(TextReader reader)
        {
            Contract.Requires(reader != null);
            Contract.Ensures(Contract.Result<FloorDesigner>() != null);

            return CreateSerializer().Deserialize<Container>(reader).Unwrap();
        }

        /// <summary>
        /// Parameters passed into the wall growth generator
        /// </summary>
        internal class GrowthParameters
        {
            /// <summary>
            /// Parameters for parallelism checking
            /// </summary>
            public ParallelCheckParameters? ParallelCheck { get; [UsedImplicitly] set; }

            /// <summary>
            /// Distance between seeds along walls
            /// </summary>
            public object SeedSpacing { get; [UsedImplicitly] set; }

            /// <summary>
            /// Chance that a seed will be placed at a seed point (otherwise the wall will continue straight on)
            /// </summary>
            public object SeedChance { get; [UsedImplicitly] set; }

            /// <summary>
            /// When a wall hits another wall chance that the wall will continue out the other side
            /// </summary>
            public object IntersectionContinuationChance { get; [UsedImplicitly] set; }
        }

        internal struct ParallelCheckParameters
        {
            /// <summary>
            /// Multiplier of length to check for parallel walls (e.g. 1.5 to check for the length of the proposed wall and half again)
            /// </summary>
            public object Length { get; set; }

            /// <summary>
            /// Distance to check for parallel walls either side
            /// </summary>
            public object Width { get; set; }

            /// <summary>
            /// Maximum angle between this wall and the other to consider parallel
            /// </summary>
            public object Angle { get; set; }
        }

        internal class Container
            : IUnwrappable<FloorDesigner>
        {
            // ReSharper disable MemberCanBePrivate.Global
            // ReSharper disable CollectionNeverUpdated.Global
            public List<object> Aliases { get; [UsedImplicitly] set; }
            public Dictionary<string, string> Tags { get; [UsedImplicitly] set; }
            public string Id { get; [UsedImplicitly] set; }
            public string Description { get; [UsedImplicitly] set; }

            public GrowthParameters GrowthParameters { get; [UsedImplicitly] set; }

            public List<BaseSpaceSpec.BaseContainer> Spaces { get; [UsedImplicitly] set; }
            // ReSharper restore CollectionNeverUpdated.Global
            // ReSharper restore MemberCanBePrivate.Global

            public FloorDesigner Unwrap()
            {
                Contract.Assert(GrowthParameters != null
                             && GrowthParameters.SeedSpacing != null
                );

                var defaultParallel = new ParallelCheckParameters {
                    Length = 1.25f,
                    Width = 1,
                    Angle = 10
                };
                var parallelParams = GrowthParameters.ParallelCheck ?? defaultParallel;

                return new FloorDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description ?? "",
                    Spaces.Select(a => a.Unwrap()).ToArray(),
                    IValueGeneratorContainer.FromObject(GrowthParameters.SeedSpacing, new NormallyDistributedValue(2.5f, 5, 6.5f, 1.5f)),
                    IValueGeneratorContainer.FromObject(parallelParams.Length, defaultParallel.Length),
                    IValueGeneratorContainer.FromObject(parallelParams.Width, defaultParallel.Width),
                    IValueGeneratorContainer.FromObject(parallelParams.Angle, defaultParallel.Angle).Transform(Microsoft.Xna.Framework.MathHelper.ToRadians),
                    IValueGeneratorContainer.FromObject(GrowthParameters.IntersectionContinuationChance, 0.75f),
                    IValueGeneratorContainer.FromObject(GrowthParameters.SeedChance, 0.5f)
                );
            }
        }
        #endregion
    }
}
