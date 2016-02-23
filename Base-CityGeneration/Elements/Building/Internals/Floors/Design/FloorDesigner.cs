using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces;
using Base_CityGeneration.Utilities.Numbers;
using JetBrains.Annotations;
using Myre.Collections;
using SharpYaml.Serialization;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design
{
    public class FloorDesigner
    {
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

        #region fields and properties
        private readonly IReadOnlyList<BaseSpaceSpec> _spaces;
        #endregion

        #region constructor
        private FloorDesigner(Dictionary<string, string> tags, Guid guid, string description, IReadOnlyList<BaseSpaceSpec> spaces)
        {
            _tags = tags;
            _guid = guid;
            _description = description;
            _spaces = spaces;
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
            //Collection of unused objects, helpful for writing scripts
            public List<object> Aliases { get; [UsedImplicitly] set; }

            // ReSharper disable once CollectionNeverUpdated.Global
            // ReSharper disable once MemberCanBePrivate.Global
            public Dictionary<string, string> Tags { get; [UsedImplicitly] set; }

            // ReSharper disable once MemberCanBePrivate.Global
            public string Id { get; [UsedImplicitly] set; }

            // ReSharper disable once MemberCanBePrivate.Global
            public string Description { get; [UsedImplicitly] set; }

            // ReSharper disable once CollectionNeverUpdated.Global
            // ReSharper disable once MemberCanBePrivate.Global
            public List<BaseSpaceSpec.BaseContainer> Spaces { get; [UsedImplicitly] set; }

            public FloorDesigner Unwrap()
            {
                return new FloorDesigner(
                    Tags,
                    Guid.Parse(Id ?? Guid.NewGuid().ToString()),
                    Description ?? "",
                    Spaces.Select(a => a.Unwrap()).ToArray()
                );
            }
        }
        #endregion
    }
}
