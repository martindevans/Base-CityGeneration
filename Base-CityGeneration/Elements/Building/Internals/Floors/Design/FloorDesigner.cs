using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Planning;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan;
using Base_CityGeneration.Elements.Building.Internals.Floors.Plan.Geometric;
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
            get
            {
                Contract.Ensures(Contract.Result<Dictionary<string, string>>() != null);
                return _tags;
            }
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

        private readonly WallGrowthParameters _wallGrowthParameters;
        private readonly FloorPlanner.MergingParameters _roomMergeParameters;
        private readonly FloorPlanner.CorridorParameters _corridorParameters;
        #endregion

        #region constructor
        private FloorDesigner(Dictionary<string, string> tags, Guid guid, string description, IReadOnlyList<BaseSpaceSpec> spaces, WallGrowthParameters wallGrowthParameters, FloorPlanner.MergingParameters roomMergeParameters, FloorPlanner.CorridorParameters corridorParameters)
        {
            Contract.Requires(wallGrowthParameters != null);
            Contract.Requires(spaces != null);
            Contract.Requires(tags != null);

            _tags = tags;
            _guid = guid;
            _description = description;
            _spaces = spaces;

            _wallGrowthParameters = wallGrowthParameters;
            _roomMergeParameters = roomMergeParameters;
            _corridorParameters = corridorParameters;
        }
        #endregion

        #region design
        public IFloorPlanBuilder Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IReadOnlyList<Vector2> footprint, IReadOnlyList<IReadOnlyList<Subsection>> sections, float wallThickness, IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<ConstrainedVerticalSelection> startingVerticals)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(footprint != null && footprint.Count >= 3);
            Contract.Requires(sections != null && sections.Count == footprint.Count);
            Contract.Requires(overlappingVerticals != null && Contract.ForAll(overlappingVerticals, o => o != null));
            Contract.Requires(startingVerticals != null && Contract.ForAll(startingVerticals, s => s != null));
            Contract.Ensures(Contract.Result<IFloorPlanBuilder>() != null);

            var plan = new GeometricFloorplan(footprint);
            Design(random, metadata, finder, plan, sections, wallThickness, overlappingVerticals, startingVerticals);

            return plan;
        }

        public void Design(Func<double> random, INamedDataCollection metadata, Func<KeyValuePair<string, string>[], Type[], ScriptReference> finder, IFloorPlanBuilder builder, IReadOnlyList<IReadOnlyList<Subsection>> sections, float wallThickness, IReadOnlyList<IReadOnlyList<Vector2>> overlappingVerticals, IReadOnlyList<ConstrainedVerticalSelection> startingVerticals)
        {
            Contract.Requires(random != null);
            Contract.Requires(metadata != null);
            Contract.Requires(finder != null);
            Contract.Requires(builder != null);
            Contract.Requires(sections != null && sections.Count == builder.ExternalFootprint.Count);
            Contract.Requires(overlappingVerticals != null && Contract.ForAll(overlappingVerticals, o => o != null));
            Contract.Requires(startingVerticals != null && Contract.ForAll(startingVerticals, s => s != null));

            var region = CreateRegion(builder.ExternalFootprint, sections);

            var planner = new FloorPlanner(random, metadata, finder, wallThickness, _wallGrowthParameters, _roomMergeParameters, _corridorParameters);
            planner.Plan(builder, region, overlappingVerticals, startingVerticals, _spaces);
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

        internal class Container
            : IUnwrappable<FloorDesigner>
        {
            // ReSharper disable MemberCanBePrivate.Global
            // ReSharper disable CollectionNeverUpdated.Global
            public List<object> Aliases { get; [UsedImplicitly] set; }
            public Dictionary<string, string> Tags { get; [UsedImplicitly] set; }
            public string Id { get; [UsedImplicitly] set; }
            public string Description { get; [UsedImplicitly] set; }

            public WallGrowthParameters.Container GrowthParameters { get; [UsedImplicitly] set; }
            public FloorPlanner.MergingParameters.Container MergeParameters { get; [UsedImplicitly] set; }
            public FloorPlanner.CorridorParameters.Container CorridorParameters { get; [UsedImplicitly] set; }

            public List<BaseSpaceSpec.BaseContainer> Spaces { get; [UsedImplicitly] set; }
            // ReSharper restore CollectionNeverUpdated.Global
            // ReSharper restore MemberCanBePrivate.Global

            public FloorDesigner Unwrap()
            {
                return new FloorDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description ?? "",
                    Spaces.Select(a => a.Unwrap()).ToArray(),
                    GrowthParameters.Unwrap(),
                    FloorPlanner.MergingParameters.Container.UnwrapDefault(MergeParameters),
                    FloorPlanner.CorridorParameters.Container.UnwrapDefault(CorridorParameters)
                );
            }
        }
        #endregion
    }
}
