using System.Collections.Generic;
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

        /// <summary>
        /// Indicates if this space may be used to connect to other spaces (i.e. people may walk through this space to get to the spaces)
        /// </summary>
        public bool Walkthrough { get; private set; }

        /// <summary>
        /// Indicates if entry elements (vertical elements or external doors) may be attached directly to this space (e.g. some kind of lobby)
        /// </summary>
        public bool EntrySpace { get; private set; }

        public RoomSpec(string id, IEnumerable<KeyValuePair<float, KeyValuePair<string, string>[]>> tags, bool walkthrough, bool entrySpace)
            : base(id)
        {
            Tags = tags;

            Walkthrough = walkthrough;
            EntrySpace = entrySpace;
        }

        internal class Container
            : BaseContainer
        {
            public TagContainerContainer Tags { get; [UsedImplicitly] set; }

            //todo: public List<object> Constraints { get; [UsedImplicitly] set; }
            //todo: public List<object> Connections { get; [UsedImplicitly] set; }

            public bool Walkthrough { get; [UsedImplicitly]set; }
            public bool EntrySpace { get; [UsedImplicitly]set; }

            public override BaseSpaceSpec Unwrap()
            {
                return new RoomSpec(
                    Id,
                    Tags.Unwrap(),
                    Walkthrough,
                    EntrySpace
                    //Connections,
                    //Constraints
                );
            }
        }
    }
}
