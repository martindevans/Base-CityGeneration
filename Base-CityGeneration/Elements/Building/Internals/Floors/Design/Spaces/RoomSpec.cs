using System.Collections.Generic;
using System.Linq;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Constraints;
using Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces.Selector;
using Base_CityGeneration.Utilities;
using JetBrains.Annotations;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    public class RoomSpec
        : BaseSpaceSpec, ISpaceSpec
    {
        /// <summary>
        /// Tags to use for this room (keyed by relative probability)
        /// </summary>
        public IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> Tags { get; private set; }

        public bool Walkthrough { get; private set; }

        public bool EntrySpace { get; private set; }

        public IEnumerable<Weighted<BaseConstraint>> Constraints { get; private set; }

        public IEnumerable<Weighted<BaseSpecSelector>> Connections { get; private set; }

        public RoomSpec(string id, IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> tags, bool walkthrough, bool entrySpace, IReadOnlyList<Weighted<BaseConstraint>> constraints, IReadOnlyList<Weighted<BaseSpecSelector>> connections)
            : base(id)
        {
            Tags = tags;

            Walkthrough = walkthrough;
            EntrySpace = entrySpace;
            Constraints = constraints;
            Connections = connections;
        }

        internal class Container
            : BaseContainer
        {
            // ReSharper disable CollectionNeverUpdated.Global
            // ReSharper disable MemberCanBePrivate.Global
            public TagContainerContainer Tags { get; [UsedImplicitly] set; }

            public List<Weighted<BaseConstraint>.Container<BaseConstraint.BaseContainer>> Constraints { get; [UsedImplicitly] set; }
            public List<Weighted<BaseSpecSelector>.Container<BaseSpecSelector.BaseContainer>> Connections { get; [UsedImplicitly] set; }

            public bool Walkthrough { get; [UsedImplicitly]set; }
            public bool EntrySpace { get; [UsedImplicitly]set; }
            // ReSharper restore MemberCanBePrivate.Global
            // ReSharper restore CollectionNeverUpdated.Global

            public override BaseSpaceSpec Unwrap()
            {
                return new RoomSpec(
                    Id,
                    Tags.Unwrap(),
                    Walkthrough,
                    EntrySpace,
                    Constraints.UnwrapEnumerable().ToArray(),
                    Connections.UnwrapEnumerable().ToArray()
                );
            }
        }
    }
}
