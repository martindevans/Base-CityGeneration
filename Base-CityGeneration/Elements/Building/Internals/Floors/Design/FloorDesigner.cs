using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Utilities.Numbers;
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

        private readonly IReadOnlyCollection<BaseSpaceSpec> _rooms;
        public IReadOnlyCollection<BaseSpaceSpec> Rooms
        {
            get { return _rooms; }
        }
        #endregion

        private FloorDesigner(Dictionary<string, string> tags, Guid id, string description, IReadOnlyCollection<BaseSpaceSpec> rooms)
        {
            Tags = tags;
            Id = id;
            Description = description;

            _rooms = rooms;
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

            //Constraints
            //serializer.Settings.RegisterTagMapping("Exterior", typeof(...));

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
            public List<object> Aliases { get; [UsedImplicitly]set; }

            // ReSharper disable once CollectionNeverUpdated.Global
            public Dictionary<string, string> Tags { get; [UsedImplicitly]set; }
            public string Id { get; [UsedImplicitly]set; }
            public string Description { get; [UsedImplicitly]set; }

            public BaseSpaceSpec.BaseContainer[] Rooms{get; [UsedImplicitly]set;}

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
