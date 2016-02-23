using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Myre.Collections;

namespace Base_CityGeneration.Elements.Building.Internals.Floors.Design.Spaces
{
    /// <summary>
    /// A group is laid out as if it were a room, and then is expanded and runs it's own floorplan generation in itself
    /// </summary>
    public class GroupSpec
        : BaseSpaceSpec, IProviderSpec, ISpaceSpec
    {
        private readonly IReadOnlyList<ISpec> _specs;

        /// <summary>
        /// Indicates if this space may be used to connect to other spaces (i.e. people may walk through this space to get to the spaces)
        /// </summary>
        public bool Walkthrough { get; private set; }

        /// <summary>
        /// Indicates if entry elements (vertical elements or external doors) may be attached directly to this space (e.g. some kind of lobby)
        /// </summary>
        public bool EntrySpace { get; private set; }

        public GroupSpec(string id, bool walkthrough, bool entrySpace, IReadOnlyList<ISpec> specs)
            : base(id)
        {
            _specs = specs;

            Walkthrough = walkthrough;
            EntrySpace = entrySpace;
        }

        public IEnumerable<ISpec> Expand(Func<double> random, INamedDataCollection metadata)
        {
            return _specs;
        }

        internal class Container
            : BaseContainer
        {
            //todo: public List<object> Constraints { get; [UsedImplicitly] set; }
            //todo: public List<object> Connections { get; [UsedImplicitly] set; }

            public bool Walkthrough { get; [UsedImplicitly]set; }
            public bool EntrySpace { get; [UsedImplicitly]set; }

            // ReSharper disable once CollectionNeverUpdated.Global
            public List<BaseContainer> Children { get; [UsedImplicitly]set; }

            public override BaseSpaceSpec Unwrap()
            {
                return new GroupSpec(Id, Walkthrough, EntrySpace, Children.Select(a => a.Unwrap()).ToArray());
            }
        }
    }
}
