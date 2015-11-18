using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Connections;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Constraints;
using Base_CityGeneration.Utilities;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public class RoomSpec
        : BaseSpaceSpec
    {
        public IReadOnlyList<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags { get; private set; }

        public RoomSpec(IReadOnlyList<KeyValuePair<float, KeyValuePair<string, string>[]>> tags, string id, bool walkthrough, IReadOnlyList<RequirementStrength<BaseSpaceConstraintSpec>> constraints, IReadOnlyList<RequirementStrength<BaseSpaceConnectionSpec>> connections)
            : base(id, walkthrough, constraints, connections)
        {
            Tags = tags;
        }

        internal class Container
            : BaseContainer
        {
            // ReSharper disable once CollectionNeverUpdated.Global
            public TagContainerContainer Tags { get; [UsedImplicitly] set; }

            internal override BaseSpaceSpec Unwrap()
            {
                return new RoomSpec(
                    Tags.Unwrap().ToArray(),
                    Id,
                    Walkthrough,
                    (Constraints ?? NoConstraints).Select(a => a.Unwrap()).ToArray(),
                    (Connections ?? NoConnections).Select(a => a.Unwrap()).ToArray()
                );
            }
        }
    }
}
