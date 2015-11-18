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
using JetBrains.Annotations;
using SharpYaml.Serialization;

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
        #endregion

        private FloorDesigner(Dictionary<string, string> tags, Guid id, string description, IReadOnlyCollection<ISpaceSpecProducer> rooms)
        {
            Tags = tags;
            Id = id;
            Description = description;

            _rooms = rooms;
        }

        public FloorPlan Design(IReadOnlyList<Vector2> footprint)
        {
            using (var skeleton = StraightSkeleton.Generate(footprint))
            {
                //check if skeleton is too small, or twoo far from walls - if it is generate an offset skeleton

                //Connect external doors to hallway
                //Connect vertical features to hallway
                //  - Either create them on the corridor
                //  - Or create a new corridor to the vertical

                //Split space into regions (bounded by hallways)

                //Place rooms and shuffle to maximise satisfied constraints (this may be a little complex!)

                //If a space is passthrough merge it into adjacent hallways and expand it to fill space
            }

            return new FloorPlan(footprint);
        }

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

            public FloorDesigner Unwrap()
            {
                return new FloorDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description,
                    Rooms.Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}
